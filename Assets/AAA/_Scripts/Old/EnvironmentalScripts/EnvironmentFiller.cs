using System.Collections.Generic;
using UnityEngine;

public class EnvironmentFiller : MonoBehaviour
{
    [Header("Building Pool")]
    public List<GameObject> buildingPrefabs = new List<GameObject>();

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 20f;
    [SerializeField] private int fillRadius = 4;

    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 0.75f;
    [SerializeField] private float groundY = 0f;

    private static readonly int[] RotationAngles = { 0, 90, 180, 270 };

    public void Fill(List<LevelPart> placedParts)
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0)
        {
            Debug.LogWarning("EnvironmentFiller: no building prefabs assigned.");
            return;
        }

        HashSet<Vector2Int> occupied = GetOccupiedCells(placedParts);
        HashSet<Vector2Int> toFill = GetFillCells(occupied);

        GameObject container = new GameObject("Environment");
        container.transform.SetParent(transform, false);

        foreach (Vector2Int cell in toFill)
        {
            if (Random.value > spawnChance) continue;

            GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
            Vector3 pos = new Vector3(cell.x * cellSize, groundY, cell.y * cellSize);
            float yRot = RotationAngles[Random.Range(0, RotationAngles.Length)];
            Quaternion rot = Quaternion.Euler(0f, yRot, 0f);
            Instantiate(prefab, pos, rot, container.transform);
        }
    }

    private HashSet<Vector2Int> GetOccupiedCells(List<LevelPart> parts)
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        foreach (LevelPart part in parts)
        {
            if (part == null) continue;

            Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                cells.Add(WorldToCell(part.transform.position));
                continue;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);

            Vector2Int min = WorldToCell(new Vector3(bounds.min.x, 0f, bounds.min.z));
            Vector2Int max = WorldToCell(new Vector3(bounds.max.x, 0f, bounds.max.z));

            for (int x = min.x; x <= max.x; x++)
                for (int z = min.y; z <= max.y; z++)
                    cells.Add(new Vector2Int(x, z));
        }
        return cells;
    }

    private HashSet<Vector2Int> GetFillCells(HashSet<Vector2Int> occupied)
    {
        HashSet<Vector2Int> fill = new HashSet<Vector2Int>();
        foreach (Vector2Int cell in occupied)
        {
            for (int dx = -fillRadius; dx <= fillRadius; dx++)
            {
                for (int dz = -fillRadius; dz <= fillRadius; dz++)
                {
                    Vector2Int candidate = new Vector2Int(cell.x + dx, cell.y + dz);
                    if (!occupied.Contains(candidate))
                        fill.Add(candidate);
                }
            }
        }
        return fill;
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / cellSize),
            Mathf.FloorToInt(worldPos.z / cellSize)
        );
    }
}
