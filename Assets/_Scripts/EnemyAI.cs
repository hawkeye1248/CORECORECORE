using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    public enum NonAlertBehaviourType {Sentry, RandomPatrol, DeterminedPatrol};
    [SerializeField] private NonAlertBehaviourType nonAlertBehaviourType;

    public enum EnemyState { Idle, Shock, Alert }
    [SerializeField] private EnemyState currentState;

    [Header("General Variables")]
    private NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;
    private Vector3 startingPos;

    [Header("Patrol Parameters")]
    public float walkPointRange;
    private Vector3 walkPoint;
    [SerializeField] private bool walkPointSet;
    [SerializeField] private List<Transform> patrolPoints;
    private int currentPatrolIndex = 0;
    [SerializeField] private float sightRange;

    [Header("Shock Parameters")]
    [SerializeField] private GameObject alertIcon;
    private float shockTimer = 0f;
    private float shockMaxTime = 1f;
    [Header("Attack Parameters")]
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private float missileSpeed;
    [SerializeField] private float missileDamage;
    private float nextFireTime;
    [SerializeField]private float shotsPerSecond;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        startingPos = transform.position;
    }

    void Start()
    {
        player = Player.Instance.transform;
        currentState = EnemyState.Idle;
    }

    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Shock:
                Shock();
                break;
            case EnemyState.Alert:
                Alert();
                break;
            default:
                break;
        }
    }

    private void Idle()
    {
        if(Physics.CheckSphere(transform.position, sightRange, whatIsPlayer))
        {
            currentState = EnemyState.Shock;
        }

        switch (nonAlertBehaviourType)
        {
            case NonAlertBehaviourType.Sentry:
                agent.SetDestination(startingPos);
                break;
            case NonAlertBehaviourType.RandomPatrol:
                if (!walkPointSet)
                {
                    SearchWalkPoint();
                }

                if (walkPointSet)
                {
                    agent.SetDestination(walkPoint);
                    Vector3 distanceToWalkPoint = transform.position - walkPoint;
                    if (distanceToWalkPoint.magnitude < 2f)
                        walkPointSet = false;
                }
                break;
            case NonAlertBehaviourType.DeterminedPatrol:
                if (patrolPoints == null || patrolPoints.Count == 0)
                {
                    return;
                }

                Transform targetPoint = patrolPoints[currentPatrolIndex];
                agent.SetDestination(targetPoint.position);
                Vector3 distanceToTarget = transform.position - targetPoint.position;
                if (distanceToTarget.magnitude < 2f)
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
                }
                break;
            default:
                break;
        }
    }

    private void SearchWalkPoint()
    {
        for (int i = 0; i < 10; i++) 
        {
            float randomZ = Random.Range(-walkPointRange, walkPointRange);
            float randomX = Random.Range(-walkPointRange, walkPointRange);
            Vector3 randomPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, walkPointRange, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    walkPoint = hit.position;
                    walkPointSet = true;
                    return;
                }
            }
        }
    }

    private void Shock()
    {
        alertIcon.SetActive(true);
        shockTimer += Time.deltaTime;
        if(shockTimer > shockMaxTime)
        {
            currentState = EnemyState.Alert;
            alertIcon.SetActive(false);
            shockTimer = 0;
        }
    }
    
    private void Alert()
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + (1.0f / shotsPerSecond);
            GameObject missile = Instantiate(missilePrefab, transform.position, Quaternion.identity);
            missile.GetComponent<FPSRetroKit.Missile>().speed = missileSpeed;
            missile.GetComponent<FPSRetroKit.Missile>().damage = missileDamage;
        }
    }

}
