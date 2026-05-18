using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public List<LevelPart> parts = new List<LevelPart>();
    [SerializeField] private int numberOfParts = 10;
    [SerializeField] private float interpartSpace = 10f;
    private LevelPart lastInstantiatedPart;

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

        // Instantiate the first part
        LevelPart firstPart = Instantiate(parts[0], transform.position, Quaternion.identity, transform);
        Vector3 offset = transform.position - firstPart.Entrance.position;
        firstPart.transform.position += offset;
        lastInstantiatedPart = firstPart;

        for (int i = 1; i < numberOfParts; i++)
        {
            LevelPart newPartPrefab = parts[Random.Range(0, parts.Count)];
            LevelPart newPart = Instantiate(newPartPrefab, lastInstantiatedPart.Exit.position, Quaternion.identity, transform);
            offset = lastInstantiatedPart.Exit.position - newPart.Entrance.position;
            newPart.transform.position += offset + lastInstantiatedPart.Exit.forward * interpartSpace;
            lastInstantiatedPart = newPart;
        }
    }
}
