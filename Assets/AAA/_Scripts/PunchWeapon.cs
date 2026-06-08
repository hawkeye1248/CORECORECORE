using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using MovementRework;

public class PunchWeapon : MonoBehaviour
{
    [Header("Weapon Properties")]
    public float fireInterval = 0.2f;
    [SerializeField] private float range = 3f;
    [SerializeField] private float totalAngle = 90f;
    private int numberOfCasts = 10;
    [SerializeField] private float boxWidth = 1f;
    [SerializeField] private float boxHeight = 1f;
    public LayerMask targetLayer;
    
    [SerializeField] public bool isFireInterval = false;
    private HashSet<NewEnemyTest> hitEnemies = new HashSet<NewEnemyTest>();
    private float closestEnemyDistance = int.MaxValue;
    private EnemyBodyPartScript closestEnemyPart = null;
    private Animator anim;
    [SerializeField] private float hitstopTime = 0.1f;
    [SerializeField] private float prehitstopTime = 0.1f;
    [SerializeField] private float hitstopAmount = 0.1f;


    [SerializeField] private float minKnockbackForce;
    [SerializeField] private float maxKnockbackForce;

    private void Start() {
        anim = GetComponent<Animator>();
    }

    void Update()
    {

    }

    public void Shoot(Vector3 pos, Quaternion rot)
    {
        if(isFireInterval)
        {
            return;
        }

        if(anim.GetCurrentAnimatorStateInfo(layerIndex:0).IsName("Punch R"))
        {
            anim.SetTrigger("Punching2");
        } else if(anim.GetCurrentAnimatorStateInfo(layerIndex:0).IsName("Punch L"))
        {
            anim.SetTrigger("Punching2");
        } else 
        {
            int randomNum = Random.Range(0, 2);
            if(randomNum == 0)
            {
                anim.SetTrigger("PunchingRight");
            } else
            {
                anim.SetTrigger("PunchingLeft");
            }
            
        }
        

        
        
        StartCoroutine(FireInterval());
        
    }

    public void Punch()
    {
        hitEnemies.Clear();
        closestEnemyDistance = int.MaxValue;
        closestEnemyPart = null;
        float startAngle = -totalAngle / 2f;
        float angleStep = totalAngle / (numberOfCasts - 1);

        for (int i = 0; i < numberOfCasts; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            
            Vector3 centerOffset = rotation * Vector3.forward * (range / 2f);
            Vector3 boxCenter = transform.position + centerOffset;
            Vector3 halfExtents = new Vector3(boxWidth / 2f, boxHeight / 2f, range / 2f);

            Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfExtents, rotation, targetLayer);

            for (int j = 0; j < hitColliders.Length; j++)
            {
                if (hitColliders[j].TryGetComponent<EnemyBodyPartScript>(out EnemyBodyPartScript enemyPart))
                {
                    if (!hitEnemies.Contains(enemyPart.enemy))
                    {
                        if((enemyPart.enemy.transform.position - transform.position).magnitude < closestEnemyDistance)
                        {
                            closestEnemyDistance = (enemyPart.enemy.transform.position - transform.position).magnitude;
                            closestEnemyPart = enemyPart;
                        }
                        
                        
                        hitEnemies.Add(enemyPart.enemy);
                    }
                } //TODO duvarlara vurma ekle
            }
            
        }

        if(closestEnemyPart != null)
        {
            closestEnemyPart.Die(Vector3.Normalize(closestEnemyPart.transform.position - transform.position), 
            Mathf.Lerp(minKnockbackForce, maxKnockbackForce, GetComponentInParent<Player>().core.linearVelocity.magnitude / 20));
            StartCoroutine(HitStop());
        }
        

        if (GetComponentInChildren<ParticleSystem>() != null)
        {
            GetComponentInChildren<ParticleSystem>().Play();
        }
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
            Vector3 boxCenter = transform.position + (rotation * Vector3.forward * (range / 2f));
            Matrix4x4 cubeMatrix = Matrix4x4.TRS(boxCenter, rotation, Vector3.one);
            Gizmos.matrix = cubeMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxWidth, boxHeight, range));
        }
    }

    public IEnumerator FireInterval()
    {
        isFireInterval = true;
        yield return new WaitForSeconds(fireInterval);
        isFireInterval = false;
    }

    public IEnumerator HitStop()
    {
        yield return new WaitForSecondsRealtime(prehitstopTime);
        Time.timeScale = hitstopAmount;
        yield return new WaitForSecondsRealtime(hitstopTime);
        Time.timeScale = 1f;
    }
}
