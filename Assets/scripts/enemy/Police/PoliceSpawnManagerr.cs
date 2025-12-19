using System.Collections.Generic;
using UnityEngine;

public class PoliceSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject policePrefab;
    public List<Transform> spawnPoints;
    public int policeToSpawn = 5;
    public float minSpawnDistance = 20f; // Minimum distance from player

    [Header("Player Reference")]
    public PlayerMovement playerController; // Reference to your script with isWanted

    private bool hasSpawned = false;
    private List<GameObject> spawnedPolice = new List<GameObject>();

    private void Update()
    {
        // Check if player is wanted and we haven't spawned yet
        if (playerController != null && playerController.isWanted && !hasSpawned)
        {
            SpawnPolice();
            hasSpawned = true;
        }

        // Optional: Despawn police if player is no longer wanted
        if (playerController != null && !playerController.isWanted && hasSpawned)
        {
            DespawnPolice();
            hasSpawned = false;
        }
    }

    private void SpawnPolice()
    {
        if (policePrefab == null)
        {
            Debug.LogError("Police prefab is not assigned!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }

        if (playerController == null)
        {
            Debug.LogError("Player controller is not assigned!");
            return;
        }

        // Get valid spawn points (at least minSpawnDistance away from player)
        List<Transform> validSpawnPoints = new List<Transform>();
        Vector3 playerPosition = playerController.transform.position;

        foreach (Transform spawnPoint in spawnPoints)
        {
            float distance = Vector3.Distance(playerPosition, spawnPoint.position);
            if (distance >= minSpawnDistance)
            {
                validSpawnPoints.Add(spawnPoint);
            }
        }

        if (validSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No valid spawn points found! All spawn points are too close to the player.");
            return;
        }

        Debug.Log($"Player is wanted! Spawning {policeToSpawn} police officers from {validSpawnPoints.Count} valid spawn points.");

        for (int i = 0; i < policeToSpawn; i++)
        {
            // Pick a random valid spawn point
            Transform spawnPoint = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];

            // Spawn the police officer
            GameObject police = Instantiate(policePrefab, spawnPoint.position, spawnPoint.rotation);

            // Set player reference if the police has a PoliceManager component
            PoliceManager policeManager = police.GetComponent<PoliceManager>();
            if (policeManager != null && playerController != null)
            {
                policeManager.player = playerController.gameObject;
            }

            spawnedPolice.Add(police);
            Debug.Log($"Spawned police #{i + 1} at {spawnPoint.name} (Distance: {Vector3.Distance(playerPosition, spawnPoint.position):F1} units)");
        }
    }

    private void DespawnPolice()
    {
        Debug.Log("Player no longer wanted. Despawning police.");

        foreach (GameObject police in spawnedPolice)
        {
            if (police != null)
            {
                Destroy(police);
            }
        }

        spawnedPolice.Clear();
    }

    // Optional: Manual spawn/despawn for testing
    [ContextMenu("Force Spawn Police")]
    public void ForceSpawn()
    {
        if (!hasSpawned)
        {
            SpawnPolice();
            hasSpawned = true;
        }
    }

    [ContextMenu("Force Despawn Police")]
    public void ForceDespawn()
    {
        if (hasSpawned)
        {
            DespawnPolice();
            hasSpawned = false;
        }
    }
}