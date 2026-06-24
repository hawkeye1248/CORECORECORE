using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    private const float StepHeight = 20f;

    [Header("Grid")]
    [SerializeField] private float unitSize = 10f;

    [Header("Base Tier")]
    [SerializeField] private int minBaseUnitsX = 3;
    [SerializeField] private int maxBaseUnitsX = 6;
    [SerializeField] private int minBaseUnitsZ = 2;
    [SerializeField] private int maxBaseUnitsZ = 5;
    [SerializeField] private int minBaseLayers = 1;
    [SerializeField] private int maxBaseLayers = 4;

    [Header("Upper Tiers")]
    [SerializeField] private int minTiers = 1;
    [SerializeField] private int maxTiers = 4;
    [SerializeField] private bool allowOffset = true;
    [SerializeField] private int minShrinkUnits = 2;
    [SerializeField] private int maxShrinkUnits = 4;
    [SerializeField] private int minOffsetUnits = 1;
    [SerializeField] private int maxOffsetUnits = 3;

    [Header("Wings")]
    [SerializeField] private bool allowWings = true;
    [SerializeField] private int maxWings = 2;
    [SerializeField] private int minWingSpanUnits = 2;
    [SerializeField] private int maxWingSpanUnits = 5;
    [SerializeField] private int minWingLengthUnits = 2;
    [SerializeField] private int maxWingLengthUnits = 6;

    [Header("Appearance")]
    [SerializeField] private bool extrudeToGround = true;
    [SerializeField] private Material buildingMaterial;
    [SerializeField] public bool generateOnStart = true;

    private struct TierData
    {
        public float CenterX, CenterZ, Width, Depth, BottomY, Height;
    }

    public struct PieceInfo
    {
        public float TopY;
        public float RightFaceX;
        public float LeftFaceX;
    }

    private readonly List<GameObject> _pieces = new List<GameObject>();

    public List<PieceInfo> GetPieceInfos()
    {
        var result = new List<PieceInfo>();
        foreach (GameObject piece in _pieces)
        {
            Transform t = piece.transform;
            Vector3 wp = t.position;
            Vector3 ws = t.lossyScale;
            result.Add(new PieceInfo
            {
                TopY       = wp.y + ws.y * 0.5f,
                RightFaceX = wp.x + ws.x * 0.5f,
                LeftFaceX  = wp.x - ws.x * 0.5f
            });
        }
        return result;
    }

    private void Awake()
    {
        if (generateOnStart)
            Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        Clear();

        float bottomY = 0f;
        float centerX = 0f;
        float centerZ = 0f;

        float w = Random.Range(minBaseUnitsX, maxBaseUnitsX + 1) * unitSize;
        float d = Random.Range(minBaseUnitsZ, maxBaseUnitsZ + 1) * unitSize;
        float baseH = Random.Range(minBaseLayers, maxBaseLayers + 1) * StepHeight;

        SpawnCube(centerX, bottomY + baseH * 0.5f, centerZ, w, baseH, d);
        List<TierData> placedTiers = new List<TierData>
        {
            new TierData { CenterX = centerX, CenterZ = centerZ, Width = w, Depth = d, BottomY = bottomY, Height = baseH }
        };
        bottomY += baseH;

        int tiers = Random.Range(minTiers, maxTiers + 1);
        for (int i = 0; i < tiers; i++)
        {
            if (w <= unitSize && d <= unitSize) break;

            bool useOffset = allowOffset && Random.value > 0.5f;

            if (useOffset)
            {
                float shrinkW = Random.value > 0.5f ? Random.Range(minShrinkUnits, maxShrinkUnits + 1) * unitSize : 0f;
                float shrinkD = Random.value > 0.5f ? Random.Range(minShrinkUnits, maxShrinkUnits + 1) * unitSize : 0f;
                w = Mathf.Max(w - shrinkW, unitSize);
                d = Mathf.Max(d - shrinkD, unitSize);

                int oxSteps = Random.Range(-maxOffsetUnits, maxOffsetUnits + 1);
                int ozSteps = Random.Range(-maxOffsetUnits, maxOffsetUnits + 1);
                if (oxSteps == 0 && ozSteps == 0) oxSteps = minOffsetUnits;
                oxSteps = oxSteps < 0 ? Mathf.Min(oxSteps, -minOffsetUnits) : (oxSteps > 0 ? Mathf.Max(oxSteps, minOffsetUnits) : 0);
                ozSteps = ozSteps < 0 ? Mathf.Min(ozSteps, -minOffsetUnits) : (ozSteps > 0 ? Mathf.Max(ozSteps, minOffsetUnits) : 0);

                centerX += oxSteps * unitSize;
                centerZ += ozSteps * unitSize;
            }
            else
            {
                float shrink = Random.Range(minShrinkUnits, maxShrinkUnits + 1) * unitSize;
                w = Mathf.Max(w - shrink, unitSize);
                d = Mathf.Max(d - shrink, unitSize);
            }

            SpawnCube(centerX, bottomY + StepHeight * 0.5f, centerZ, w, StepHeight, d);
            placedTiers.Add(new TierData { CenterX = centerX, CenterZ = centerZ, Width = w, Depth = d, BottomY = bottomY, Height = StepHeight });
            bottomY += StepHeight;
        }

        if (allowWings)
        {
            // Only tiers tall enough to place a wing one full step below their ceiling
            List<TierData> wingCandidates = placedTiers.FindAll(t => t.BottomY + t.Height > StepHeight);
            int wingCount = Random.Range(0, maxWings + 1);
            for (int i = 0; i < wingCount; i++)
            {
                if (wingCandidates.Count == 0) break;
                TierData tier = wingCandidates[Random.Range(0, wingCandidates.Count)];
                SpawnWing(tier);
            }
        }

        if (extrudeToGround)
            ExtrudeToGround();
    }

    private void ExtrudeToGround()
    {
        foreach (GameObject piece in _pieces)
        {
            Transform t = piece.transform;
            float top = t.localPosition.y + t.localScale.y * 0.5f;
            t.localScale = new Vector3(t.localScale.x, top, t.localScale.z);
            t.localPosition = new Vector3(t.localPosition.x, top * 0.5f, t.localPosition.z);
        }
    }

    private void SpawnWing(TierData tier)
    {
        int side = Random.Range(0, 4); // 0=+X, 1=-X, 2=+Z, 3=-Z

        float span   = Random.Range(minWingSpanUnits,   maxWingSpanUnits   + 1) * unitSize;
        float length = Random.Range(minWingLengthUnits, maxWingLengthUnits + 1) * unitSize;

        float wingW, wingD, wingX, wingZ;

        switch (side)
        {
            case 0: // +X
                wingW = span;
                wingD = Mathf.Min(length, tier.Depth);
                wingX = tier.CenterX + tier.Width * 0.5f + wingW * 0.5f;
                wingZ = tier.CenterZ + Random.Range(-0.5f, 0.5f) * (tier.Depth - wingD);
                break;
            case 1: // -X
                wingW = span;
                wingD = Mathf.Min(length, tier.Depth);
                wingX = tier.CenterX - (tier.Width * 0.5f + wingW * 0.5f);
                wingZ = tier.CenterZ + Random.Range(-0.5f, 0.5f) * (tier.Depth - wingD);
                break;
            case 2: // +Z
                wingW = Mathf.Min(length, tier.Width);
                wingD = span;
                wingX = tier.CenterX + Random.Range(-0.5f, 0.5f) * (tier.Width - wingW);
                wingZ = tier.CenterZ + tier.Depth * 0.5f + wingD * 0.5f;
                break;
            default: // -Z
                wingW = Mathf.Min(length, tier.Width);
                wingD = span;
                wingX = tier.CenterX + Random.Range(-0.5f, 0.5f) * (tier.Width - wingW);
                wingZ = tier.CenterZ - (tier.Depth * 0.5f + wingD * 0.5f);
                break;
        }

        // Wing top sits one step below the tier's ceiling
        float wingTopY = tier.BottomY + tier.Height - StepHeight;
        SpawnCube(wingX, wingTopY - StepHeight * 0.5f, wingZ, wingW, StepHeight, wingD);
    }

    private void SpawnCube(float x, float y, float z, float w, float h, float d)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform, false);
        cube.transform.localPosition = new Vector3(x, y, z);
        cube.transform.localScale = new Vector3(w, h, d);

        if (buildingMaterial != null)
            cube.GetComponent<MeshRenderer>().sharedMaterial = buildingMaterial;

        _pieces.Add(cube);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        foreach (GameObject p in _pieces)
            if (p != null) DestroyImmediate(p);
        _pieces.Clear();

        // Also clean up any leftover children from prior runs
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }
}
