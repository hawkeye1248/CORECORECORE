using System;
using System.Collections;
using System.Collections.Generic;
using AAA._Scripts.AnimationRelated.MCAnimations;
using AAA._Scripts.Enums;
using MovementRework;
using UnityEngine;

public class PlayerWeaponManager : MonoBehaviour
{
    public MovementRework.Player playerScript;
    private PlayerAnimations playerAnimScript;
    private Transform mainCam;
    private WeaponScript currentWeapon;

    [Header("Punch Properties")]
    [SerializeField] private MeleeShapeData punchShape;
    [SerializeField] private float punchDamage = 30f;
    public float fireInterval = 0.2f;
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

    [Header("Weapon Pickup")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private LayerMask weaponLayer;
    private GameObject lastWeaponLooked;

    private void Start()
    {
        mainCam = playerScript.GetCamera();
        playerAnimScript = GetComponentInChildren<PlayerAnimations>();
        MovementInput.Instance.OnRMBPerformed += on_RMB_performed;
        MovementInput.Instance.OnLMBPerformed += on_LMB_performed;
        MovementInput.Instance.OnInteractPerformed += OnInteractPerformed;

        if (weaponHolder != null && weaponHolder.GetComponentInChildren<WeaponScript>() != null)
        {
            currentWeapon = weaponHolder.GetComponentInChildren<WeaponScript>();
        }
    }

    private void Update()
    {
        UpdateWeaponPickupRaycast();
    }

    private void UpdateWeaponPickupRaycast()
    {
        RaycastHit hit;
        if (Physics.Raycast(mainCam.position, mainCam.forward, out hit, 3f, weaponLayer))
        {
            if (hit.collider.gameObject != lastWeaponLooked)
            {
                if (lastWeaponLooked != null)
                {
                    lastWeaponLooked.GetComponent<Outline>().enabled = false;
                }
                hit.collider.gameObject.GetComponent<Outline>().enabled = true;
                lastWeaponLooked = hit.collider.gameObject;
            }
        }
        else
        {
            if (lastWeaponLooked != null)
            {
                lastWeaponLooked.GetComponent<Outline>().enabled = false;
                lastWeaponLooked = null;
            }
        }
    }

    private void OnInteractPerformed(object sender, EventArgs e)
    {
        if (currentWeapon != null) return;

        if (Physics.Raycast(mainCam.position, mainCam.forward, out RaycastHit hit, 3f, weaponLayer))
        {
            hit.transform.GetComponent<WeaponScript>().Pickup(weaponHolder);
            currentWeapon = hit.transform.GetComponent<WeaponScript>();
            playerAnimScript.CurrentPlayerWeapon = currentWeapon.WeaponData.weaponType;
        }
    }

    private void on_LMB_performed(object sender, EventArgs e)
    {
        if (currentWeapon != null)
        {
            currentWeapon.Shoot(SpawnPos(), mainCam.rotation, false);
        }
        else
        {
            TryPunchLeft();
        }
        playerAnimScript.Lmb();
    }

    private void on_RMB_performed(object sender, EventArgs e)
    {
        if (currentWeapon != null)
        {
            RaycastHit castHit;
            if (Physics.Raycast(mainCam.position, mainCam.forward, out castHit, 100))
            {
                currentWeapon.isEquippedByPlayer = false;
                currentWeapon.Throw(castHit.point);
                currentWeapon = null;
                playerAnimScript.CurrentPlayerWeapon = Weapon.None;
            }
            else
            {
                currentWeapon.isEquippedByPlayer = false;
                currentWeapon.Throw(mainCam.position + (mainCam.forward * 100));
                currentWeapon = null;
                playerAnimScript.CurrentPlayerWeapon = Weapon.None;
            }
        }
        else
        {
            TryPunchRight();
        }
        playerAnimScript.Rmb();
    }

    private void TryPunchRight()
    {
        if (isFireInterval)
        {
            return;
        }

        //playerScript.playerModel.PunchRightTrigger();
        StartCoroutine(FireInterval());
    }

    private void TryPunchLeft()
    {
        if (isFireInterval)
        {
            return;
        }

        //playerScript.playerModel.PunchLeftTrigger();
        StartCoroutine(FireInterval());
    }

    public void Punch()
    {
        hitEnemies.Clear();
        closestEnemyDistance = int.MaxValue;
        closestEnemyPart = null;
        float startAngle = -punchShape.totalAngle / 2f;
        float angleStep = punchShape.totalAngle / (punchShape.castCount - 1);

        for (int i = 0; i < punchShape.castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = playerScript.cameraController.transform.rotation * Quaternion.Euler(0, currentAngle, 0);

            Vector3 centerOffset = rotation * Vector3.forward * (punchShape.range / 2f);
            Vector3 boxCenter = playerScript.cameraController.transform.position + centerOffset;
            Vector3 halfExtents = new Vector3(punchShape.boxWidth / 2f, punchShape.boxHeight / 2f, punchShape.range / 2f);

            Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfExtents, rotation, targetLayer);

            for (int j = 0; j < hitColliders.Length; j++)
            {
                if (hitColliders[j].TryGetComponent<EnemyBodyPartScript>(out EnemyBodyPartScript enemyPart))
                {
                    if (!hitEnemies.Contains(enemyPart.enemy))
                    {
                        if ((enemyPart.enemy.transform.position - transform.position).magnitude < closestEnemyDistance)
                        {
                            closestEnemyDistance = (enemyPart.enemy.transform.position - transform.position).magnitude;
                            closestEnemyPart = enemyPart;
                        }

                        hitEnemies.Add(enemyPart.enemy);
                    }
                }
            }
        }

        if (closestEnemyPart != null)
        {
            closestEnemyPart.ApplyDamage(punchDamage, Vector3.Normalize(closestEnemyPart.transform.position - transform.position),
                Mathf.Lerp(minKnockbackForce, maxKnockbackForce, playerScript.core.linearVelocity.magnitude / 20));
            StartCoroutine(playerScript.playerModel.WeaponHitStop(hitstopTime, hitstopAmount));
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
        if (punchShape == null) return;
        Gizmos.color = Color.red;
        float startAngle = -punchShape.totalAngle / 2f;
        float angleStep = punchShape.totalAngle / (punchShape.castCount - 1);
        for (int i = 0; i < punchShape.castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = playerScript.cameraController.transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            Vector3 boxCenter = playerScript.cameraController.transform.position + (rotation * Vector3.forward * (punchShape.range / 2f));
            Matrix4x4 cubeMatrix = Matrix4x4.TRS(boxCenter, rotation, Vector3.one);
            Gizmos.matrix = cubeMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(punchShape.boxWidth, punchShape.boxHeight, punchShape.range));
        }
    }

    public IEnumerator HitStop()
    {
        yield return new WaitForSecondsRealtime(prehitstopTime);
        Time.timeScale = 0.25f;
        yield return new WaitForSecondsRealtime(hitstopTime);
        Time.timeScale = 1f;
    }
}
