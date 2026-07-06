using UnityEngine;
using System.Collections.Generic;
using MovementRework;

public class MeleeWeapon : WeaponScript
{
    [Header("Melee Detection")]
    public LayerMask targetLayer;
    private HashSet<NewEnemyTest> hitEnemies = new HashSet<NewEnemyTest>();


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        weaponCollider = GetComponent<Collider>();
    }
    public override void Shoot(Vector3 pos, Quaternion rot, bool isEnemy)
    {
        if (isFireInterval)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            return;
        }

        hitEnemies.Clear();
        float startAngle = -WeaponData.meleeShape.totalAngle / 2f;
        float angleStep = WeaponData.meleeShape.totalAngle / (WeaponData.meleeShape.castCount - 1);
        for (int i = 0; i < WeaponData.meleeShape.castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = mainCam.rotation * Quaternion.Euler(0, currentAngle, 0);

            Vector3 centerOffset = rotation * Vector3.forward * (WeaponData.meleeShape.range / 2f);
            Vector3 boxCenter = mainCam.position + centerOffset;
            Vector3 halfExtents = new Vector3(WeaponData.meleeShape.boxWidth / 2f, WeaponData.meleeShape.boxHeight / 2f, WeaponData.meleeShape.range / 2f);

            Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfExtents, rotation, targetLayer);

            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<EnemyBodyPartScript>(out EnemyBodyPartScript enemyPart))
                {
                    if (!hitEnemies.Contains(enemyPart.enemy))
                    {
                        enemyPart.ApplyDamage(WeaponData.damage, mainCam.forward, Mathf.Lerp(WeaponData.minKnockbackForce, WeaponData.maxKnockbackForce, Player.Instance.core.linearVelocity.magnitude / 20));
                        currentAmmo--;
                        hitEnemies.Add(enemyPart.enemy);
                    }
                }
            }
        }

        if (GetComponentInChildren<ParticleSystem>() != null)
        {
            GetComponentInChildren<ParticleSystem>().Play();
        }

        if (isEquippedByPlayer)
        {
            StartCoroutine(FireInterval());
        }
    }

    void OnDrawGizmos()
    {
        if (mainCam == null || WeaponData == null || WeaponData.meleeShape == null) return;

        Gizmos.color = Color.red;
        float startAngle = -WeaponData.meleeShape.totalAngle / 2f;
        float angleStep = WeaponData.meleeShape.totalAngle / (WeaponData.meleeShape.castCount - 1);
        for (int i = 0; i < WeaponData.meleeShape.castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = mainCam.rotation * Quaternion.Euler(0, currentAngle, 0);
            Vector3 boxCenter = mainCam.position + (rotation * Vector3.forward * (WeaponData.meleeShape.range / 2f));
            Matrix4x4 cubeMatrix = Matrix4x4.TRS(boxCenter, rotation, Vector3.one);
            Gizmos.matrix = cubeMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(WeaponData.meleeShape.boxWidth, WeaponData.meleeShape.boxHeight, WeaponData.meleeShape.range));
        }
    }
}
