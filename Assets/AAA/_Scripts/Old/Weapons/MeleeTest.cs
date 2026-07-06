using UnityEngine;
using System.Collections.Generic;

public class MeleeTest : MonoBehaviour
{
    [Header("Algılama Ayarları")]
    [Range(1f, 100f)] public float castDistance = 10f;
    public float boxWidth = 1f;
    public float boxHeight = 1f;
    public LayerMask detectionLayer;

    private readonly int castCount = 10;
    private readonly float totalAngle = 90f;

    // Aynı karede birden fazla hasar almasını engellemek için set
    private HashSet<NewEnemyTest> hitEnemies = new HashSet<NewEnemyTest>();

    void Update()
    {
        // Her kare başında listeyi temizle
        hitEnemies.Clear();
        ExecuteFanDamage();
    }

    void ExecuteFanDamage()
    {
        float startAngle = -totalAngle / 2f;
        float angleStep = totalAngle / (castCount - 1);

        for (int i = 0; i < castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            
            Vector3 centerOffset = rotation * Vector3.forward * (castDistance / 2f);
            Vector3 boxCenter = transform.position + centerOffset;
            Vector3 halfExtents = new Vector3(boxWidth / 2f, boxHeight / 2f, castDistance / 2f);

            // Bu kutunun içindeki tüm collider'ları bul
            Collider[] hitColliders = Physics.OverlapBox(boxCenter, halfExtents, rotation, detectionLayer);

            foreach (var col in hitColliders)
            {
                // Nesnede NewEnemyTest scripti var mı kontrol et
                if (col.TryGetComponent<NewEnemyTest>(out NewEnemyTest enemy))
                {
                    // Eğer bu karede henüz hasar almadıysa
                    if (!hitEnemies.Contains(enemy))
                    {
                        //enemy.TakeDamage();
                        hitEnemies.Add(enemy); // Kare sonuna kadar listeye ekle
                    }
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        // Önceki görselleştirme kodunun aynısı
        Gizmos.color = Color.red;
        float startAngle = -totalAngle / 2f;
        float angleStep = totalAngle / (castCount - 1);
        for (int i = 0; i < castCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, currentAngle, 0);
            Vector3 boxCenter = transform.position + (rotation * Vector3.forward * (castDistance / 2f));
            Matrix4x4 cubeMatrix = Matrix4x4.TRS(boxCenter, rotation, Vector3.one);
            Gizmos.matrix = cubeMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxWidth, boxHeight, castDistance));
        }
    }
}
