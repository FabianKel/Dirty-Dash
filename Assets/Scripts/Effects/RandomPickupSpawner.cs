using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RandomPickupSpawner : MonoBehaviour
{
    [System.Serializable]
    public class WeightedPickup
    {
        public string id;
        public GameObject prefab;
        public float weight = 1f;
    }

    [Header("Spawn Behavior")]
    public bool spawnOnStart = true;
    public bool respawnWhenCollected = false;
    public float respawnDelay = 5f;
    public bool keepSpawnedAsChild = false;

    [Header("Pool (Blind with lower default probability)")]
    public List<WeightedPickup> pickups = new List<WeightedPickup>
    {
        new WeightedPickup { id = "Slow",   weight = 1f },
        new WeightedPickup { id = "Blind",  weight = 0.35f },
        new WeightedPickup { id = "Invert", weight = 1f },
        new WeightedPickup { id = "Boost",  weight = 1f },
    };

    GameObject _currentPickup;
    bool _waitingRespawn;

    void Start()
    {
        if (spawnOnStart)
            SpawnRandomPickup();
    }

    void Update()
    {
        if (!respawnWhenCollected) return;
        if (_currentPickup != null) return;
        if (_waitingRespawn) return;

        StartCoroutine(RespawnAfterDelay());
    }

    [ContextMenu("Spawn Random Pickup")]
    public void SpawnRandomPickup()
    {
        if (_currentPickup != null) return;

        GameObject prefab = PickRandomPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("RandomPickupSpawner: no hay prefabs validos configurados.");
            return;
        }

        Transform parent = keepSpawnedAsChild ? transform : null;
        _currentPickup = Instantiate(prefab, transform.position, Quaternion.identity, parent);
    }

    GameObject PickRandomPrefab()
    {
        float total = 0f;
        for (int i = 0; i < pickups.Count; i++)
        {
            var entry = pickups[i];
            if (entry == null || entry.prefab == null || entry.weight <= 0f) continue;
            total += entry.weight;
        }

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        float cursor = 0f;

        for (int i = 0; i < pickups.Count; i++)
        {
            var entry = pickups[i];
            if (entry == null || entry.prefab == null || entry.weight <= 0f) continue;

            cursor += entry.weight;
            if (roll <= cursor) return entry.prefab;
        }

        return null;
    }

    IEnumerator RespawnAfterDelay()
    {
        _waitingRespawn = true;
        yield return new WaitForSeconds(respawnDelay);
        _waitingRespawn = false;
        SpawnRandomPickup();
    }
}
