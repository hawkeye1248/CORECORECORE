using System.Collections;
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
                agent.SetDestination(player.position);
                if(agent.velocity.magnitude >= 0)
                {
                    enemyAnim.SetBool("Running", true);
                } else
                {
                    enemyAnim.SetBool("Running", false);
                }
                if((player.transform.position - transform.position).magnitude <= 2)
                {
                    MeleeAttack();
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
        if(!isFireCooldown)
        {
            StartCoroutine(ShootInterval());
        }
    }

    public IEnumerator ShootInterval()
    {
        isFireCooldown = true;
        enemyAnim.SetTrigger("Shoot");
        //GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint, );
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
        yield return new WaitForSeconds(fireInterval);
        isFireCooldown = true;
    }

    public void Ragdoll()
    {
        enemyAnim.enabled = false;
        agent.enabled = false;
        EnemyBodyPartScript[] parts = GetComponentsInChildren<EnemyBodyPartScript>();
        foreach (EnemyBodyPartScript bp in parts)
        {
            bp.rb.isKinematic = false;
            bp.rb.interpolation = RigidbodyInterpolation.Interpolate;
            bp.gameObject.layer = 10;
        }
        isDead = true;
    }
}
