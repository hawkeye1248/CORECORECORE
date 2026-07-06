using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthData", menuName = "Data/Player Health Data")]
public class PlayerHealthData : HealthData
{
    [Header("Health Drain")]
    public bool doesDrainHealth = false;
    public float healthDrainTickTime = 0.25f;
    public float healthDrainPerTick = 1f;

    [Header("Heal On Kill")]
    public float healOnKill = 50f;
}
