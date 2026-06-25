using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Required Parts")]
    public LevelPart startPart;
    public LevelPart endPart;

    [Header("Part Pool")]
    public List<LevelPart> parts = new List<LevelPart>();

    [Header("Generation Settings")]
    [SerializeField] private int numberOfParts = 10;

    [Header("Height Budget")]
    [SerializeField] private float minHeight = -20f;
    [SerializeField] private float maxHeight = 30f;

    private readonly List<LevelPart> _placed = new List<LevelPart>();
    private readonly List<PartCategory> _recentCategories = new List<PartCategory>();

    private void Awake()
    {
        GenerateLevel();
    }

    private void GenerateLevel()
    {
        if (parts == null || parts.Count == 0)
        {
            Debug.LogError("Level parts list is empty. Cannot generate level.");
            return;
        }

        LevelPart firstPrefab = startPart != null ? startPart : parts[0];
        LevelPart first = Instantiate(firstPrefab, transform.position, Quaternion.identity, transform);
        AlignToOrigin(first);
        _placed.Add(first);

        for (int i = 0; i < numberOfParts; i++)
        {
            LevelPart last = _placed[^1];
            float currentHeight = last.Exit.position.y - transform.position.y;

            List<LevelPart> pool = BuildPool(currentHeight);
            if (pool.Count == 0)
            {
                Debug.LogWarning("No valid parts after filtering — using full pool.");
                pool = parts;
            }

            LevelPart prefab = WeightedRandom(pool);
            LevelPart next = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            AlignExitToEntrance(last, next);
            _placed.Add(next);

            _recentCategories.Add(prefab.category);
            if (_recentCategories.Count > 5)
                _recentCategories.RemoveAt(0);
        }

        if (endPart != null)
        {
            LevelPart last = _placed[^1];
            LevelPart end = Instantiate(endPart, Vector3.zero, Quaternion.identity, transform);
            AlignExitToEntrance(last, end);
            _placed.Add(end);
        }

        GetComponent<EnvironmentFiller>()?.Fill(_placed);
    }

    private List<LevelPart> BuildPool(float currentHeight)
    {
        return parts.Where(p =>
        {
            float projected = currentHeight + p.ExitHeightDelta;
            if (projected > maxHeight) return false;
            if (projected < minHeight) return false;

            int consecutive = _recentCategories.Count > 0
                ? _recentCategories.Reverse<PartCategory>().TakeWhile(c => c == p.category).Count()
                : 0;
            if (consecutive >= p.maxConsecutive) return false;

            return true;
        }).ToList();
    }

    private static LevelPart WeightedRandom(List<LevelPart> pool)
    {
        float total = pool.Sum(p => p.weight);
        float roll = Random.Range(0f, total);
        float acc = 0f;
        foreach (LevelPart p in pool)
        {
            acc += p.weight;
            if (roll <= acc) return p;
        }
        return pool[^1];
    }

    private static void AlignToOrigin(LevelPart part)
    {
        Vector3 offset = part.transform.position - part.Entrance.position;
        part.transform.position += offset;
    }

    private static void AlignExitToEntrance(LevelPart previous, LevelPart next)
    {
        next.transform.rotation = previous.Exit.rotation;
        Vector3 entranceOffset = next.transform.position - next.Entrance.position;
        next.transform.position = previous.Exit.position + entranceOffset;
    }
}
