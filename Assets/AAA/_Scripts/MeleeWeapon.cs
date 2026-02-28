using UnityEngine;
using System.Collections.Generic;

public class MeleeWeapon : WeaponScript
{
    [Header("Weapon Properties")]
    [SerializeField] private float range = 3f;
    [SerializeField] private float totalAngle = 90f;
    private int numberOfCasts = 10;
    [SerializeField] private float boxWidth = 1f;
    [SerializeField] private float boxHeight = 1f;
    public LayerMask targetLayer;
    private HashSet<NewEnemyTest> hitEnemies = new HashSet<NewEnemyTest>();

    public override void Shoot(Vector3 pos, Quaternion rot, bool isEnemy)
    {
        if(isFireInterval)
        {
            return;
        }

        if(bulletAmount <= 0)
        {
            return;
        }

        hitEnemies.Clear();
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

            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<EnemyBodyPartScript>(out EnemyBodyPartScript enemyPart))
                {
                    if (!hitEnemies.Contains(enemyPart.enemy))
                    {
                        enemyPart.Die(Vector3.Normalize(enemyPart.transform.position - transform.position),  Mathf.Lerp(minKnockbackForce, maxKnockbackForce, GetComponentInParent<CharacterController>().velocity.magnitude / 20));
                        bulletAmount--;
                        hitEnemies.Add(enemyPart.enemy);
                    }
                } //TODO duvarlara vurma ekle
            }
        }

        if (GetComponentInChildren<ParticleSystem>() != null)
        {
            GetComponentInChildren<ParticleSystem>().Play();
        }

        if(isEquippedByPlayer)
        {
            StartCoroutine(FireInterval());
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
}
