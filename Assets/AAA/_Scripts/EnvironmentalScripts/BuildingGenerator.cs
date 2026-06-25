using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    [SerializeField] private float StepHeight = 20f;

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

    [Header("Complexity")]
    [Range(0.1f, 4f)]
    [SerializeField] private float complexityBias = 2f;

    [Header("Wings")]
    [SerializeField] private bool allowWings = true;
    [SerializeField] private int maxWings = 2;
    [SerializeField] private int minWingSpanUnits = 2;
    [SerializeField] private int maxWingSpanUnits = 5;
    [SerializeField] private int minWingLengthUnits = 2;
    [SerializeField] private int maxWingLengthUnits = 6;

    [Header("Appearance")]
    [SerializeField] private bool extrudeToGround = true;
    [SerializeField] private float embeddingDepth = 2f;
    [SerializeField] private Material buildingMaterial;
    [SerializeField] private string pieceLayerName = "Default";
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

        int tiers = BiasedRange(minTiers, maxTiers, complexityBias);
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

                // Clamp so the new tier always overlaps the tier below by at least unitSize
                TierData below = placedTiers[placedTiers.Count - 1];
                float maxDX = (below.Width + w) * 0.5f - unitSize;
                float maxDZ = (below.Depth + d) * 0.5f - unitSize;
                centerX = Mathf.Clamp(centerX, below.CenterX - maxDX, below.CenterX + maxDX);
                centerZ = Mathf.Clamp(centerZ, below.CenterZ - maxDZ, below.CenterZ + maxDZ);
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
            int wingCount = BiasedRange(0, maxWings, complexityBias);

            // Build all (tier, side) slots, shuffle, then take wingCount unique ones
            var slots = new List<(TierData tier, int side)>();
            foreach (TierData t in wingCandidates)
                for (int s = 0; s < 4; s++)
                    slots.Add((t, s));

            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            int toSpawn = Mathf.Min(wingCount, slots.Count);
            for (int i = 0; i < toSpawn; i++)
                SpawnWing(slots[i].tier, slots[i].side);
        }

        if (extrudeToGround)
            ExtrudeToGround();
    }

    private void ExtrudeToGround()
    {
        const float groundY = -20f;
        foreach (GameObject piece in _pieces)
        {
            Transform t = piece.transform;
            float top = t.localPosition.y + t.localScale.y * 0.5f;
            float newHeight = top - groundY;
            t.localScale = new Vector3(t.localScale.x, newHeight, t.localScale.z);
            t.localPosition = new Vector3(t.localPosition.x, groundY + newHeight * 0.5f, t.localPosition.z);
        }
    }

    private void SpawnWing(TierData tier, int side) {

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

        // Embed the inner face into the building to prevent coplanar z-fighting
        switch (side)
        {
            case 0: wingX -= embeddingDepth * 0.5f; wingW += embeddingDepth; break;
            case 1: wingX += embeddingDepth * 0.5f; wingW += embeddingDepth; break;
            case 2: wingZ -= embeddingDepth * 0.5f; wingD += embeddingDepth; break;
            default: wingZ += embeddingDepth * 0.5f; wingD += embeddingDepth; break;
        }

        // Sink the wing top by embeddingDepth so it doesn't share a face with the tier below
        float wingTopY = tier.BottomY + tier.Height - StepHeight - embeddingDepth;
        float wingH    = StepHeight - embeddingDepth;
        SpawnCube(wingX, wingTopY - wingH * 0.5f, wingZ, wingW, wingH, wingD);
    }

    private void SpawnCube(float x, float y, float z, float w, float h, float d)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(transform, false);
        cube.transform.localPosition = new Vector3(x, y, z);
        cube.transform.localScale = new Vector3(w, h, d);

        if (buildingMaterial != null)
            cube.GetComponent<MeshRenderer>().sharedMaterial = buildingMaterial;

        int layer = LayerMask.NameToLayer(pieceLayerName);
        cube.layer = layer >= 0 ? layer : 0;
        _pieces.Add(cube);
    }

    // bias > 1 favors min, bias < 1 favors max, bias = 1 is uniform
    private static int BiasedRange(int min, int max, float bias)
    {
        float t = Mathf.Pow(Random.value, bias);
        return Mathf.Clamp(min + Mathf.FloorToInt(t * (max - min + 1)), min, max);
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
