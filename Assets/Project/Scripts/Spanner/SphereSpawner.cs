using UnityEngine;
using System.Collections.Generic;

public class MoleculeSpawner : MonoBehaviour
{
    [Header("Main Sphere")]
    public Transform mainSphere;

    [Header("Molecule Prefabs")]
    public List<GameObject> moleculePrefabs;

    [Header("Spawn Settings")]
    public int spawnCount = 10;
    public float radius = 2f;

    [Header("Movement Settings")]
    public float moveSpeed = 1f;
    public float changeDirectionTime = 2f;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<Vector3> directions = new List<Vector3>();

    void OnEnable()
    {
        SpawnMolecules();
    }

    void Update()
    {
        MoveMolecules();
    }

    void SpawnMolecules()
    {
        // Clear old
        foreach (var obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }

        spawnedObjects.Clear();
        directions.Clear();

        for (int i = 0; i < spawnCount; i++)
        {
            if (moleculePrefabs.Count == 0) return;

            GameObject prefab = moleculePrefabs[Random.Range(0, moleculePrefabs.Count)];

            Vector3 randomPos = mainSphere.position + Random.insideUnitSphere * radius;

            GameObject obj = Instantiate(prefab, randomPos, Quaternion.identity, mainSphere);

            spawnedObjects.Add(obj);

            // Random initial direction
            directions.Add(Random.onUnitSphere);
        }
    }

    void MoveMolecules()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] == null) continue;

            Transform t = spawnedObjects[i].transform;

            // Move
            t.position += directions[i] * moveSpeed * Time.deltaTime;

            // Check boundary
            float dist = Vector3.Distance(mainSphere.position, t.position);

            if (dist > radius)
            {
                // Push back inside
                Vector3 dirToCenter = (mainSphere.position - t.position).normalized;
                directions[i] = Vector3.Reflect(directions[i], dirToCenter);
            }

            // Random direction change (floating feel)
            if (Random.value < Time.deltaTime / changeDirectionTime)
            {
                directions[i] = Random.onUnitSphere;
            }
        }
    }
}
