using UnityEngine;

public enum PartCategory
{
    Straight,
    TurnLeft,
    TurnRight,
    ClimbUp,
    ClimbDown,
    Gap,
    Special
}

public class LevelPart : MonoBehaviour
{
    public Transform Entrance;
    public Transform Exit;

    public PartCategory category = PartCategory.Straight;
    public float weight = 1f;
    public int maxConsecutive = 3;

    public float ExitHeightDelta =>
        (Exit != null && Entrance != null)
            ? Exit.position.y - Entrance.position.y
            : 0f;
}
