using UnityEngine;

[CreateAssetMenu(fileName = "MeleeShape", menuName = "Data/Melee Shape")]
public class MeleeShapeData : ScriptableObject
{
    public float range = 3f;
    public float totalAngle = 90f;
    public int castCount = 10;
    public float boxWidth = 1f;
    public float boxHeight = 1f;
}
