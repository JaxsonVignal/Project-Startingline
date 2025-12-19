using UnityEngine;
using System.Collections.Generic;

public class WantedSystem : MonoBehaviour
{
    public static WantedSystem Instance { get; private set; }

    [Header("Wanted Status")]
    public bool isPlayerWanted = false;
    public float timeWanted = 0f;
    public int currentWantedLevel = 0;

    [Header("Wanted Level Thresholds")]
    [Tooltip("Time in seconds to reach each wanted level")]
    public float[] wantedLevelThresholds = { 0f, 30f, 60f, 120f, 180f }; // 5 levels (0-4)

    [Header("Police Spawning")]
    public GameObject policePrefab;
    public List<Transform> policeSpawnPoints = new List<Transform>();
    public int[] maxPolicePerLevel = { 2, 4, 6, 8, 12 }; // Max police at each wanted level
    public float[] spawnIntervalPerLevel = { 15f, 10f, 7f, 5f, 3f }; // Spawn interval at each level
    public float minSpawnDistance = 20f; // Minimum distance from player to spawn

    [Header("Player Reference")]
    public GameObject player;

    private List<GameObject> activePolice = new List<GameObject>();
    private float spawnTimer = 0f;
    private int lastWantedLevel = 0;
    private bool hasSpawnedInitial = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Auto-find player if not set
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("WantedSystem: Player not found! Please assign player or tag player with 'Player' tag.");
            }
        }

        // Validate setup
        if (policePrefab == null)
        {
            Debug.LogError("WantedSystem: Police prefab not assigned!");
        }

        if (policeSpawnPoints.Count == 0)
        {
            Debug.LogError("WantedSystem: No spawn points assigned!");
        }
    }

    private void Update()
    {
        if (isPlayerWanted)
        {
            // Spawn initial police immediately when first becoming wanted
            if (!hasSpawnedInitial)
            {
                Debug.Log("Player just became wanted! Spawning initial police.");
                SpawnPolice();
                hasSpawnedInitial = true;
            }

            // Increase time wanted
            timeWanted += Time.deltaTime;

            // Update wanted level based on time
            UpdateWantedLevel();

            // Handle police spawning
            HandlePoliceSpawning();

            // Clean up destroyed police from list
            CleanupPoliceList();
        }
        else
        {
            // Reset when no longer wanted
            if (hasSpawnedInitial)
            {
                hasSpawnedInitial = false;
            }
        }
    }

    private void UpdateWantedLevel()
    {
        int newLevel = 0;

        // Determine wanted level based on time wanted
        for (int i = 0; i < wantedLevelThresholds.Length; i++)
        {
            if (timeWanted >= wantedLevelThresholds[i])
            {
                newLevel = i;
            }
        }

        // Clamp to max level
        newLevel = Mathf.Min(newLevel, maxPolicePerLevel.Length - 1);

        // Check if level increased
        if (newLevel > lastWantedLevel)
        {
            OnWantedLevelIncreased(lastWantedLevel, newLevel);
        }

        currentWantedLevel = newLevel;
        lastWantedLevel = newLevel;
    }

    private void HandlePoliceSpawning()
    {
        // Remove null references (destroyed police)
        activePolice.RemoveAll(p => p == null);

        int maxPolice = maxPolicePerLevel[currentWantedLevel];
        float spawnInterval = spawnIntervalPerLevel[currentWantedLevel];

        // Check if we need to spawn more police
        if (activePolice.Count < maxPolice)
        {
            spawnTimer += Time.deltaTime;

            if (spawnTimer >= spawnInterval)
            {
                SpawnPolice();
                spawnTimer = 0f;
            }
        }
    }

    private void SpawnPolice()
    {
        if (policePrefab == null)
        {
            Debug.LogError("WantedSystem: Police prefab is not assigned!");
            return;
        }

        if (policeSpawnPoints.Count == 0)
        {
            Debug.LogError("WantedSystem: No spawn points assigned!");
            return;
        }

        if (player == null)
        {
            Debug.LogError("WantedSystem: Player reference is null!");
            return;
        }

        // Get valid spawn points (at least minSpawnDistance away from player)
        List<Transform> validSpawnPoints = new List<Transform>();
        Vector3 playerPosition = player.transform.position;

        foreach (Transform spawnPoint in policeSpawnPoints)
        {
            if (spawnPoint == null) continue;

            float distance = Vector3.Distance(playerPosition, spawnPoint.position);
            if (distance >= minSpawnDistance)
            {
                validSpawnPoints.Add(spawnPoint);
            }
        }

        if (validSpawnPoints.Count == 0)
        {
            Debug.LogWarning("WantedSystem: No valid spawn points found! All spawn points are too close to the player.");
            // Fall back to using any spawn point
            validSpawnPoints = new List<Transform>(policeSpawnPoints);
        }

        // Choose a random valid spawn point
        Transform chosenSpawnPoint = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];

        // Instantiate police
        GameObject newPolice = Instantiate(policePrefab, chosenSpawnPoint.position, chosenSpawnPoint.rotation);

        // Setup police
        PoliceManager policeManager = newPolice.GetComponent<PoliceManager>();
        if (policeManager != null)
        {
            policeManager.player = player;
            policeManager.spawnPoint = chosenSpawnPoint;
            policeManager.currentState = PoliceManager.PoliceState.Searching;
        }
        else
        {
            Debug.LogWarning("WantedSystem: Spawned police doesn't have PoliceManager component!");
        }

        // Add to active police list
        activePolice.Add(newPolice);

        Debug.Log($"WantedSystem: Spawned police officer at {chosenSpawnPoint.name}. Active police: {activePolice.Count}/{maxPolicePerLevel[currentWantedLevel]}");
    }

    private void CleanupPoliceList()
    {
        activePolice.RemoveAll(p => p == null);
    }

    private void OnWantedLevelIncreased(int oldLevel, int newLevel)
    {
        Debug.Log($"WantedSystem: WANTED LEVEL INCREASED: {oldLevel} -> {newLevel}");

        // Spawn immediate reinforcements when level increases
        int reinforcements = Mathf.Min(2, maxPolicePerLevel[newLevel] - activePolice.Count);
        for (int i = 0; i < reinforcements; i++)
        {
            SpawnPolice();
        }
    }

    // PUBLIC METHODS TO CONTROL WANTED STATUS

    public void SetWanted(bool wanted)
    {
        if (wanted && !isPlayerWanted)
        {
            // Player just became wanted
            isPlayerWanted = true;
            timeWanted = 0f;
            currentWantedLevel = 0;
            lastWantedLevel = 0;
            spawnTimer = 0f;
            hasSpawnedInitial = false; // Will trigger initial spawn in Update

            Debug.Log("WantedSystem: Player is now WANTED!");
        }
        else if (!wanted && isPlayerWanted)
        {
            // Player is no longer wanted
            ClearWantedStatus();
        }
    }

    public void ClearWantedStatus()
    {
        isPlayerWanted = false;
        timeWanted = 0f;
        currentWantedLevel = 0;
        lastWantedLevel = 0;
        spawnTimer = 0f;
        hasSpawnedInitial = false;

        Debug.Log("WantedSystem: Wanted status CLEARED!");

        // Despawn all police
        DespawnAllPolice();
    }

    private void DespawnAllPolice()
    {
        Debug.Log($"WantedSystem: Despawning {activePolice.Count} police officers.");

        foreach (GameObject police in activePolice)
        {
            if (police != null)
            {
                Destroy(police);
            }
        }

        activePolice.Clear();
    }

    public void AddWantedTime(float seconds)
    {
        if (isPlayerWanted)
        {
            timeWanted += seconds;
        }
    }

    // UTILITY METHODS

    public int GetActivePoliceCount()
    {
        CleanupPoliceList();
        return activePolice.Count;
    }

    public void RemovePolice(GameObject police)
    {
        activePolice.Remove(police);
    }

    // Manual testing methods
    [ContextMenu("Force Set Wanted")]
    public void ForceSetWanted()
    {
        SetWanted(true);
    }

    [ContextMenu("Force Clear Wanted")]
    public void ForceClearWanted()
    {
        ClearWantedStatus();
    }

    [ContextMenu("Force Spawn Police Now")]
    public void ForceSpawnPolice()
    {
        SpawnPolice();
    }

    // DEBUG INFO
    private void OnGUI()
    {
        if (!isPlayerWanted) return;

        GUI.Box(new Rect(10, 10, 250, 120), "WANTED SYSTEM");
        GUI.Label(new Rect(20, 35, 230, 20), $"Wanted: {isPlayerWanted}");
        GUI.Label(new Rect(20, 55, 230, 20), $"Time Wanted: {timeWanted:F1}s");
        GUI.Label(new Rect(20, 75, 230, 20), $"Wanted Level: {currentWantedLevel}");
        GUI.Label(new Rect(20, 95, 230, 20), $"Active Police: {GetActivePoliceCount()}/{maxPolicePerLevel[currentWantedLevel]}");
        GUI.Label(new Rect(20, 115, 230, 20), $"Next spawn: {spawnIntervalPerLevel[currentWantedLevel] - spawnTimer:F1}s");
    }
}