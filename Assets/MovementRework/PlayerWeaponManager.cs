using System;
using System.Collections;
using System.Collections.Generic;
using MovementRework;
using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    public MovementRework.Player playerScript;
    private Transform mainCam;
    private WeaponScript currentWeapon;

    [Header("Punch Properties")]
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
    [SerializeField] private float hitstopAmount = 0.1f;
    [SerializeField] private float prehitstopTime = 0.1f;

    [SerializeField] private float minKnockbackForce;
    [SerializeField] private float maxKnockbackForce;
    


    private void Start() {
        mainCam = playerScript.GetCamera();

        MovementInput.Instance.OnRMBPerformed += on_RMB_performed;
        MovementInput.Instance.OnLMBPerformed += on_LMB_performed;

        //GameEvents.OnEnemyDeathWithoutWeapon += on_EnemyDeathWithoutWeapon;
        //GameEvents.OnEnemyDeathWithWeapon += on_EnemyDeathWithWeapon;
    }

    private void Update()
    {
        
    }

    private void on_LMB_performed(object sender, EventArgs e)
    {
        if(currentWeapon != null)
        {
            currentWeapon.Shoot(SpawnPos(), mainCam.rotation, false);
        } else
        {
            TryPunchLeft();
        }
    }

    private void on_RMB_performed(object sender, EventArgs e)
    {
        if(currentWeapon != null)
        {
            RaycastHit castHit;
                if(Physics.Raycast(mainCam.position, mainCam.forward, out castHit, 100))
                {
                    currentWeapon.isEquippedByPlayer = false;
                    currentWeapon.Throw(castHit.point);
                    currentWeapon = null;
                } else
                {
                    currentWeapon.isEquippedByPlayer = false;
                    currentWeapon.Throw(mainCam.position + (mainCam.forward * 100));
                    currentWeapon = null;
                }
        } else
        {
            TryPunchRight();
        }
    }

    private void TryPunchRight()
    {
        if(isFireInterval)
        {
            return;
        }

        playerScript.playerModel.PunchRightTrigger();

        StartCoroutine(FireInterval());
    }

    private void TryPunchLeft()
    {
        if(isFireInterval)
        {
            return;
        }   

        playerScript.playerModel.PunchLeftTrigger();

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
            Quaternion rotation = playerScript.cameraController.transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            
            Vector3 centerOffset = rotation * Vector3.forward * (range / 2f);
            Vector3 boxCenter = playerScript.cameraController.transform.position + centerOffset;
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
            Mathf.Lerp(minKnockbackForce, maxKnockbackForce, playerScript.core.linearVelocity.magnitude / 20));
            StartCoroutine(playerScript.playerModel.WeaponHitStop(hitstopTime, hitstopAmount));
            //StartCoroutine(HitStop());
        }
        

        if (GetComponentInChildren<ParticleSystem>() != null)
        {
            GetComponentInChildren<ParticleSystem>().Play();
        }
    }

    public IEnumerator FireInterval()
    {
        isFireInterval = true;
        yield return new WaitForSeconds(fireInterval);
        isFireInterval = false;
    }

    Vector3 SpawnPos()
    {
        return mainCam.position + (mainCam.forward * .5f) + (mainCam.up * -.02f);
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
            Quaternion rotation = playerScript.cameraController.transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            Vector3 boxCenter = playerScript.cameraController.transform.position + (rotation * Vector3.forward * (range / 2f));
            Matrix4x4 cubeMatrix = Matrix4x4.TRS(boxCenter, rotation, Vector3.one);
            Gizmos.matrix = cubeMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxWidth, boxHeight, range));
        }
    }

    public IEnumerator HitStop()
    {
        yield return new WaitForSecondsRealtime(prehitstopTime);
        Time.timeScale = 0.25f;
        yield return new WaitForSecondsRealtime(hitstopTime);
        Time.timeScale = 1f;
    }

    private void on_EnemyDeathWithWeapon()
    {
        StartCoroutine(HitStop());
    }

    private void on_EnemyDeathWithoutWeapon()
    {
        StartCoroutine(playerScript.playerModel.WeaponHitStop(hitstopTime, hitstopAmount));
    }

}
