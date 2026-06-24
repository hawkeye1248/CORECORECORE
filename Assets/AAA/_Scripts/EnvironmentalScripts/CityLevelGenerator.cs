using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CityLevelGenerator : MonoBehaviour
{
    [Header("Buildings")]
    [SerializeField] private BuildingGenerator buildingPrefab;
    [SerializeField] private int buildingCount = 5;
    [SerializeField] private int maxGenerationAttempts = 5;

    [Header("Connection")]
    [SerializeField] private float gapSize = 30f;

    [Header("Generation")]
    [SerializeField] private bool generateOnStart = true;

    private readonly List<BuildingGenerator> _buildings = new List<BuildingGenerator>();

    private void Awake()
    {
        if (generateOnStart)
            Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        Clear();

        if (buildingPrefab == null)
        {
            Debug.LogError("CityLevelGenerator: buildingPrefab is not assigned.");
            return;
        }

        // First building at origin
        BuildingGenerator first = SpawnBuilding(Vector3.zero);
        _buildings.Add(first);

        for (int i = 1; i < buildingCount; i++)
        {
            BuildingGenerator prev = _buildings[i - 1];
            BuildingGenerator.PieceInfo exitSurf = PickExitSurface(prev);
            float targetY = exitSurf.TopY;

            // Spawn at origin, try to find a height match
            BuildingGenerator next = SpawnBuilding(Vector3.zero);
            BuildingGenerator.PieceInfo entrySurf = default;
            bool matched = false;

            for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    next.Generate();
                }

                entrySurf = FindEntrySurface(next, targetY, out matched);
                if (matched) break;
            }

            if (!matched)
                entrySurf = FindClosestEntrySurface(next, targetY);

            // Position next so its entry face is gapSize away from exit face
            float xOffset = exitSurf.RightFaceX + gapSize - entrySurf.LeftFaceX;
            next.transform.position = new Vector3(xOffset, 0f, 0f);

            ResolveOverlap(prev, next);

            _buildings.Add(next);
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        foreach (BuildingGenerator b in _buildings)
            if (b != null) DestroyImmediate(b.gameObject);
        _buildings.Clear();
    }

    private BuildingGenerator SpawnBuilding(Vector3 position)
    {
        BuildingGenerator b = Instantiate(buildingPrefab, position, Quaternion.identity, transform);
        b.generateOnStart = false;
        b.Generate();
        return b;
    }

    // The rightmost-facing piece of a placed building — defines the connection height
    private static BuildingGenerator.PieceInfo PickExitSurface(BuildingGenerator b)
    {
        return b.GetPieceInfos().OrderByDescending(p => p.RightFaceX).First();
    }

    // Piece in b whose TopY matches targetY exactly, preferring the leftmost face
    private static BuildingGenerator.PieceInfo FindEntrySurface(
        BuildingGenerator b, float targetY, out bool matched)
    {
        var candidates = b.GetPieceInfos().Where(p => Mathf.Approximately(p.TopY, targetY)).ToList();
        matched = candidates.Count > 0;
        if (matched)
            return candidates.OrderBy(p => p.LeftFaceX).First();

        return default;
    }

    // Fallback: piece with TopY closest to targetY, leftmost face
    private static BuildingGenerator.PieceInfo FindClosestEntrySurface(
        BuildingGenerator b, float targetY)
    {
        return b.GetPieceInfos()
            .OrderBy(p => Mathf.Abs(p.TopY - targetY))
            .ThenBy(p => p.LeftFaceX)
            .First();
    }

    // Push next further right if it overlaps prev in X
    private static void ResolveOverlap(BuildingGenerator prev, BuildingGenerator next)
    {
        float prevMaxX = prev.GetPieceInfos().Max(p => p.RightFaceX);
        float nextMinX = next.GetPieceInfos().Min(p => p.LeftFaceX);

        if (nextMinX < prevMaxX)
            next.transform.position += Vector3.right * (prevMaxX - nextMinX + 0.1f);
    }
}
