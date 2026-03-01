using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NewEnemyTest : MonoBehaviour
{
    Animator enemyAnim;
    public bool isDead;
    public bool isUsingPipe;
    
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;
    private Vector3 startingPos;
    private NavMeshAgent agent;
    public bool isAlarmed = false;
    [SerializeField] private float sightRange;
    public float fireInterval = 3;
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] public Transform bulletSpawnPoint;
    public bool isFireCooldown = false;
    public GameObject weaponPrefab; 

    [SerializeField] public Transform weaponHand;
    [SerializeField] public Transform weaponSpawnPoint;
    
    [SerializeField] private float range = 3f;
    [SerializeField] private float totalAngle = 90f;
    private int numberOfCasts = 10;
    [SerializeField] private float boxWidth = 1f;
    [SerializeField] private float boxHeight = 1f;
    public LayerMask targetLayer;
    private HashSet<PlayerWeaponController> hitEnemies = new HashSet<PlayerWeaponController>();

    void Awake()
    {
        enemyAnim = GetComponent<Animator>();
        enemyAnim.SetBool("IsUsingPipe", isUsingPipe);   

        agent = GetComponent<NavMeshAgent>();
        startingPos = transform.position;  
    }

    void Start()
    {
        player = PlayerWeaponController.instance.transform;
    }

    void Update()
    {
        if(isDead)
        {
            return;
        }

        if(isAlarmed)
        {
            enemyAnim.SetBool("Alarm", true);
            if(isUsingPipe)
            {
                if(agent.velocity.magnitude >= 0)
                {
                    enemyAnim.SetBool("Running", true);
                } else
                {
                    enemyAnim.SetBool("Running", false);
                }
                
                if((player.transform.position - transform.position).magnitude <= 2)
                {
                    agent.SetDestination(transform.position);
                    enemyAnim.SetBool("Running", false);
                    MeleeAttack();
                } else
                {
                    agent.SetDestination(player.position);
                }
            } else
            {

                Shoot();
            }
        } else
        {
            Idle();
        }
    }

    public void Idle()
    {
        if(Physics.CheckSphere(transform.position, sightRange, whatIsPlayer))
        {
            isAlarmed = true;
        } else
        {
            agent.SetDestination(startingPos);
        }
    }

    public void Shoot()
    {
        Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(targetPosition);
        if(!isFireCooldown)
        {
            StartCoroutine(ShootInterval());
        }
    }

    public IEnumerator ShootInterval()
    {
        isFireCooldown = true;
        enemyAnim.SetTrigger("Shoot");
        Vector3 bulletDirection = (new Vector3(player.position.x, player.position.y - 1, player.position.z) - transform.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(bulletDirection);
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, rotation);
        yield return new WaitForSeconds(fireInterval);
        isFireCooldown = false;
        
    }

    public void MeleeAttack()
    {
        if(!isFireCooldown)
        {
            StartCoroutine(MeleeInterval());
        }
    }

    public IEnumerator MeleeInterval()
    {
        isFireCooldown = true;
        int randomVal = Random.Range(0, 2);
        if(randomVal == 0)
        {
            enemyAnim.SetTrigger("PipeAttack1");
        } else
        {
            enemyAnim.SetTrigger("PipeAttack2");
        }
        //melee attack
        hitEnemies.Clear();
        float startAngle = -totalAngle / 2f;
        float angleStep = totalAngle / (numberOfCasts - 1);
        for (int i = 0; i < numberOfCasts; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            
            Vector3 centerOffset = rotation * Vector3.forward * (range / 2f);
            Vector3 boxCenter = bulletSpawnPoint.position + centerOffset;
            Vector3 halfExtents = new Vector3(boxWidth / 2f, boxHeight / 2f, range / 2f);

            Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfExtents, rotation, targetLayer);

            foreach (var col in hitColliders)
            {
                if(col.TryGetComponent<PlayerWeaponController>(out PlayerWeaponController player))
                {
                    if(!hitEnemies.Contains(player))
                    {
                        player.GetComponent<Health>().DamageHealth(30);
                        hitEnemies.Add(player);
                    }
                    
                }
            }
        }
        yield return new WaitForSeconds(fireInterval);
        isFireCooldown = false;
    }

    public void Ragdoll()
    {
        if(isDead)
        {
            return;
        }
        enemyAnim.enabled = false;
        agent.enabled = false;
        weaponHand.gameObject.SetActive(false);
        Instantiate(weaponPrefab, weaponSpawnPoint.position, Quaternion.identity);
        EnemyBodyPartScript[] parts = GetComponentsInChildren<EnemyBodyPartScript>();
        foreach (EnemyBodyPartScript bp in parts)
        {
            bp.rb.isKinematic = false;
            bp.rb.interpolation = RigidbodyInterpolation.Interpolate;
            bp.gameObject.layer = 10;
        }
        isDead = true;
    }

    void OnDrawGizmos()
    {
        // Önceki görselleştirme kodunun aynısı
        Gizmos.color = Color.red;
        float startAngle = -totalAngle / 2f;
        float angleStep = totalAngle / (numberOfCasts - 1);
        for (int i = 0; i < numberOfCasts; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            Vector3 boxCenter = bulletSpawnPoint.position + (rotation * Vector3.forward * (range / 2f));
            Matrix4x4 cubeMatrix = Matrix4x4.TRS(boxCenter, rotation, Vector3.one);
            Gizmos.matrix = cubeMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxWidth, boxHeight, range));
        }
    }
}
