using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Data/Enemy Data")]
public class EnemyDataSO : HealthData
{
    [Header("Detection")]
    public float sightRange = 10f;

    [Header("Attack")]
    public float fireInterval = 3f;
    public float damage = 30f;

    [Header("Melee")]
    public MeleeShapeData meleeShape;
}
