using UnityEngine;

[CreateAssetMenu(fileName = "HealthData", menuName = "Data/Health Data")]
public class HealthData : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;
}
