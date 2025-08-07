using System;
using System.Collections;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Common spawn points")] public Transform[] commonSpawnPoints;
    [Tooltip("Rare spawn points")] public Transform[] rareSpawnPoints;
    [Range(0f, 1f), Tooltip("Chance to pick a rare spawn point each spawn")] public float rarePointChance = 0.2f;

   
    [Serializable]
    public class AsteroidEntry
    {
        [Tooltip("The AsteroidSO ScriptableObject")] public AsteroidSO asteroidSO;
        [Tooltip("Relative weight for weighted random selection")] public float weight = 1f;
    }
    [Header("Asteroid Prefabs & Weights")]
    [Tooltip("Asteroid types and their spawn weights")] public AsteroidEntry[] asteroidEntries;

    [Header("Spawn Timing & Limits")]
    [Tooltip("Minimum time between spawn attempts (seconds)")] public float spawnIntervalMin = 1f;
    [Tooltip("Maximum time between spawn attempts (seconds)")] public float spawnIntervalMax = 3f;
    [Tooltip("Maximum number of asteroids allowed simultaneously")] public int maxAsteroids = 20;

    [Header("Spawn Collision Avoidance")]
    [Tooltip("Minimum clearance around spawn point to allow spawning")] public float spawnRadius = 2f;

    [Tooltip("Optional parent for spawned asteroids")] public Transform parent;

    private void Start()
    {
        if (parent == null) parent = transform;
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(wait);
            TrySpawn();
        }
    }

    private void TrySpawn()
    {
        // Don't spawn if at max count
        if (parent.childCount >= maxAsteroids) return;

        // Pick spawn point
        bool useRare = UnityEngine.Random.value < rarePointChance;
        Transform[] points = useRare ? rareSpawnPoints : commonSpawnPoints;
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning($"No {(useRare ? "rare" : "common")} spawn points assigned.");
            return;
        }
        Transform spawnPoint = points[UnityEngine.Random.Range(0, points.Length)];

        // Check for nearby asteroids to avoid overlap
        Collider[] hits = Physics.OverlapSphere(spawnPoint.position, spawnRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Asteroid"))
            {
                // Too close to an existing asteroid, skip spawning
                return;
            }
        }

        // Select asteroid type
        AsteroidSO chosen = SelectAsteroid();
        if (chosen == null || chosen.asteroidObject == null)
        {
            Debug.LogWarning("No asteroid entries assigned.");
            return;
        }

        // Instantiate and pass data
        GameObject go = Instantiate(chosen.asteroidObject, spawnPoint.position, spawnPoint.rotation, parent);
        Asteroid asteroidComp = go.GetComponentInChildren<Asteroid>();
        if (asteroidComp != null)
            asteroidComp.Initialize(chosen);
    }

    private AsteroidSO SelectAsteroid()
    {
        if (asteroidEntries == null || asteroidEntries.Length == 0) return null;

        float total = 0f;
        foreach (var e in asteroidEntries) total += e.weight;

        float r = UnityEngine.Random.value * total;
        float cum = 0f;
        foreach (var e in asteroidEntries)
        {
            cum += e.weight;
            if (r <= cum) return e.asteroidSO;
        }

        return asteroidEntries[0].asteroidSO;
    }

    // Optional: visualize spawn radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (commonSpawnPoints != null)
        {
            foreach (var p in commonSpawnPoints)
                Gizmos.DrawWireSphere(p.position, spawnRadius);
        }
        if (rareSpawnPoints != null)
        {
            foreach (var p in rareSpawnPoints)
                Gizmos.DrawWireSphere(p.position, spawnRadius);
        }
    }
}
