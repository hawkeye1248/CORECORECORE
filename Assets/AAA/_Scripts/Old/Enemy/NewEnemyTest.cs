using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MovementRework;

public class NewEnemyTest : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private EnemyDataSO enemyData;

    [Header("Prefabs")]
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] public Transform bulletSpawnPoint;
    [SerializeField] public GameObject weaponPrefab;
    [SerializeField] public Transform weaponHand;
    [SerializeField] public Transform weaponSpawnPoint;

    [Header("Heal Orb")]
    [SerializeField] private GameObject healOrbPrefab;

    [Header("Layers")]
    public LayerMask whatIsGround, whatIsPlayer;
    public LayerMask targetLayer;

    [Header("State")]
    public bool isUsingPipe;
    public bool isAlarmed = false;
    public bool isFireCooldown = false;

    private Animator enemyAnim;
    public Transform player;
    private Vector3 startingPos;
    private NavMeshAgent agent;
    private Health health;
    private HashSet<Player> hitEnemies = new HashSet<Player>();

    void Awake()
    {
        enemyAnim = GetComponent<Animator>();
        enemyAnim.SetBool("IsUsingPipe", isUsingPipe);

        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        health.SetHealthData(enemyData);
        startingPos = transform.position;
    }

    void Start()
    {
        player = MovementRework.Player.Instance.core.transform;
        health.OnDeath += OnHealthDeath;
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= OnHealthDeath;
    }

    void Update()
    {
        if (health.IsDead())
        {
            return;
        }

        if (isAlarmed)
        {
            enemyAnim.SetBool("Alarm", true);
            if (isUsingPipe)
            {
                if (agent.velocity.magnitude >= 0)
                {
                    enemyAnim.SetBool("Running", true);
                }
                else
                {
                    enemyAnim.SetBool("Running", false);
                }

                if ((player.transform.position - transform.position).magnitude <= 2)
                {
                    agent.SetDestination(transform.position);
                    enemyAnim.SetBool("Running", false);
                    MeleeAttack();
                }
                else
                {
                    agent.SetDestination(player.position);
                }
            }
            else
            {
                Shoot();
            }
        }
        else
        {
            Idle();
        }
    }

    public void Idle()
    {
        if (Physics.CheckSphere(transform.position, enemyData.sightRange, whatIsPlayer))
        {
            isAlarmed = true;
        }
        else
        {
            agent.SetDestination(startingPos);
        }
    }

    public void Shoot()
    {
        Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(targetPosition);
        if (!isFireCooldown)
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
        if (bullet.TryGetComponent<Bullet>(out var b))
        {
            b.source = Bullet.BulletSource.Enemy;
            b.damage = enemyData.damage;
        }
        yield return new WaitForSeconds(enemyData.fireInterval);
        isFireCooldown = false;
    }

    public void MeleeAttack()
    {
        if (!isFireCooldown)
        {
            StartCoroutine(MeleeInterval());
        }
    }

    public IEnumerator MeleeInterval()
    {
        isFireCooldown = true;
        int randomVal = Random.Range(0, 2);
        if (randomVal == 0)
        {
            enemyAnim.SetTrigger("PipeAttack1");
        }
        else
        {
            enemyAnim.SetTrigger("PipeAttack2");
        }

        hitEnemies.Clear();
        float startAngle = -enemyData.meleeShape.totalAngle / 2f;
        float angleStep = enemyData.meleeShape.totalAngle / (enemyData.meleeShape.castCount - 1);
        for (int i = 0; i < enemyData.meleeShape.castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);

            Vector3 centerOffset = rotation * Vector3.forward * (enemyData.meleeShape.range / 2f);
            Vector3 boxCenter = bulletSpawnPoint.position + centerOffset;
            Vector3 halfExtents = new Vector3(enemyData.meleeShape.boxWidth / 2f, enemyData.meleeShape.boxHeight / 2f, enemyData.meleeShape.range / 2f);

            Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfExtents, rotation, targetLayer);

            foreach (var col in hitColliders)
            {
                Player hitPlayer = col.GetComponentInParent<Player>();
                if (hitPlayer != null)
                {
                    if (!hitEnemies.Contains(hitPlayer))
                    {
                        hitPlayer.GetComponent<Health>().DamageHealth(enemyData.damage);
                        hitEnemies.Add(hitPlayer);
                    }
                }
            }
        }
        yield return new WaitForSeconds(enemyData.fireInterval);
        isFireCooldown = false;
    }

    private void OnHealthDeath(object sender, System.EventArgs e)
    {
        Ragdoll();
    }

    public void Ragdoll()
    {
        if (health.IsDead())
        {
            if (isUsingPipe)
            {
                GameEvents.OnEnemyDeathWithWeapon?.Invoke();
            }
            else
            {
                GameEvents.OnEnemyDeathWithoutWeapon?.Invoke();
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

            if (healOrbPrefab != null && player != null)
            {
                Health playerHealth = new Health();
                //Health playerHealth = MovementRework.Player.Instance.Health;
                if (playerHealth != null)
                {
                    GameObject orb = Instantiate(healOrbPrefab, transform.position + Vector3.up, Quaternion.identity);
                    orb.GetComponent<HealOrb>().Init(playerHealth, MovementRework.Player.Instance.core.transform);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (enemyData == null || enemyData.meleeShape == null) return;
        Gizmos.color = Color.red;
        float startAngle = -enemyData.meleeShape.totalAngle / 2f;
        float angleStep = enemyData.meleeShape.totalAngle / (enemyData.meleeShape.castCount - 1);
        for (int i = 0; i < enemyData.meleeShape.castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            Vector3 boxCenter = bulletSpawnPoint.position + (rotation * Vector3.forward * (enemyData.meleeShape.range / 2f));
            Matrix4x4 cubeMatrix = Matrix4x4.TRS(boxCenter, rotation, Vector3.one);
            Gizmos.matrix = cubeMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(enemyData.meleeShape.boxWidth, enemyData.meleeShape.boxHeight, enemyData.meleeShape.range));
        }
    }
}
