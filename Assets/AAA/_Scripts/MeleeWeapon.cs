using UnityEngine;
using System.Collections.Generic;
using MovementRework;

public class MeleeWeapon : WeaponScript
{
    [Header("Melee Detection")]
    public LayerMask targetLayer;
    private HashSet<NewEnemyTest> hitEnemies = new HashSet<NewEnemyTest>();

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
        float startAngle = -weaponData.meleeShape.totalAngle / 2f;
        float angleStep = weaponData.meleeShape.totalAngle / (weaponData.meleeShape.castCount - 1);
        for (int i = 0; i < weaponData.meleeShape.castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = mainCam.rotation * Quaternion.Euler(0, currentAngle, 0);

            Vector3 centerOffset = rotation * Vector3.forward * (weaponData.meleeShape.range / 2f);
            Vector3 boxCenter = mainCam.position + centerOffset;
            Vector3 halfExtents = new Vector3(weaponData.meleeShape.boxWidth / 2f, weaponData.meleeShape.boxHeight / 2f, weaponData.meleeShape.range / 2f);

            Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfExtents, rotation, targetLayer);

            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<EnemyBodyPartScript>(out EnemyBodyPartScript enemyPart))
                {
                    if (!hitEnemies.Contains(enemyPart.enemy))
                    {
                        enemyPart.ApplyDamage(weaponData.damage, mainCam.forward, Mathf.Lerp(weaponData.minKnockbackForce, weaponData.maxKnockbackForce, Player.Instance.core.linearVelocity.magnitude / 20));
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
        if (mainCam == null || weaponData == null || weaponData.meleeShape == null) return;

        Gizmos.color = Color.red;
        float startAngle = -weaponData.meleeShape.totalAngle / 2f;
        float angleStep = weaponData.meleeShape.totalAngle / (weaponData.meleeShape.castCount - 1);
        for (int i = 0; i < weaponData.meleeShape.castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = mainCam.rotation * Quaternion.Euler(0, currentAngle, 0);
            Vector3 boxCenter = mainCam.position + (rotation * Vector3.forward * (weaponData.meleeShape.range / 2f));
            Matrix4x4 cubeMatrix = Matrix4x4.TRS(boxCenter, rotation, Vector3.one);
            Gizmos.matrix = cubeMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(weaponData.meleeShape.boxWidth, weaponData.meleeShape.boxHeight, weaponData.meleeShape.range));
        }
    }
}
