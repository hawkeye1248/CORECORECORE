using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CityLevelGenerator : MonoBehaviour
{
    [Header("Buildings")]
    [SerializeField] private BuildingGenerator buildingPrefab;
    [SerializeField] private int buildingCount = 5;

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

        BuildingGenerator first = SpawnBuilding(Vector3.zero);
        _buildings.Add(first);

        for (int i = 1; i < buildingCount; i++)
        {
            BuildingGenerator prev = _buildings[i - 1];
            BuildingGenerator.PieceInfo exitSurf = PickExitSurface(prev);

            BuildingGenerator next = SpawnBuilding(Vector3.zero);
            BuildingGenerator.PieceInfo entrySurf = PickEntrySurface(next);

            // Align X so the gap between the two facing surfaces equals gapSize
            float xOffset = exitSurf.RightFaceX + gapSize - entrySurf.LeftFaceX;
            // Align Y so the entry cuboid's top matches the exit cuboid's top
            float yOffset = exitSurf.TopY - entrySurf.TopY;
            next.transform.position = new Vector3(xOffset, yOffset, 0f);

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

    // Rightmost cuboid of the previous building — its top defines the target connection height
    private static BuildingGenerator.PieceInfo PickExitSurface(BuildingGenerator b)
    {
        return b.GetPieceInfos().OrderByDescending(p => p.RightFaceX).First();
    }

    // Leftmost cuboid of the next building — the face that will point toward the previous building
    private static BuildingGenerator.PieceInfo PickEntrySurface(BuildingGenerator b)
    {
        return b.GetPieceInfos().OrderBy(p => p.LeftFaceX).First();
    }

    // Push next further right if its pieces still overlap prev in X after placement
    private static void ResolveOverlap(BuildingGenerator prev, BuildingGenerator next)
    {
        float prevMaxX = prev.GetPieceInfos().Max(p => p.RightFaceX);
        float nextMinX = next.GetPieceInfos().Min(p => p.LeftFaceX);

        if (nextMinX < prevMaxX)
            next.transform.position += Vector3.right * (prevMaxX - nextMinX + 0.1f);
    }
}
