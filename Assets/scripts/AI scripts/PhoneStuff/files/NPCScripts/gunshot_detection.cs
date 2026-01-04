using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Global system for detecting gunshots and alerting nearby NPCs
/// Add this to a GameObject in your scene (like a GameManager)
/// </summary>
public class GunshotDetectionSystem : MonoBehaviour
{
    public static GunshotDetectionSystem Instance;

    [Header("Gunshot Detection Settings")]
    [Tooltip("Maximum distance NPCs can hear gunshots")]
    public float gunshotHearingRange = 50f;

    [Tooltip("Layer mask for NPCs that can hear gunshots")]
    public LayerMask npcLayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this method whenever a gunshot occurs
    /// </summary>
    /// <param name="gunshotPosition">World position where the gunshot occurred</param>
    /// <param name="customRange">Optional custom hearing range for this specific gunshot</param>
    public void ReportGunshot(Vector3 gunshotPosition, float customRange = -1f)
    {
        float range = customRange > 0 ? customRange : gunshotHearingRange;

        // Find all NPCs within hearing range
        Collider[] nearbyColliders = Physics.OverlapSphere(gunshotPosition, range, npcLayer);

        Debug.Log($"Gunshot detected at {gunshotPosition}. Found {nearbyColliders.Length} nearby NPCs");

        foreach (Collider col in nearbyColliders)
        {
            NPCManager npc = col.GetComponent<NPCManager>();
            if (npc != null)
            {
                float distance = Vector3.Distance(gunshotPosition, npc.transform.position);
                Debug.Log($"Alerting {npc.npcName} (distance: {distance:F1}m)");
                
                // Alert the NPC about the gunshot
                npc.OnGunshotHeard(gunshotPosition);
            }
        }
    }

    /// <summary>
    /// Alternative method that searches for all NPCs (use if layer mask isn't set up)
    /// </summary>
    public void ReportGunshotToAllNPCs(Vector3 gunshotPosition, float customRange = -1f)
    {
        float range = customRange > 0 ? customRange : gunshotHearingRange;

        // Find all NPCManagers in the scene
        NPCManager[] allNPCs = FindObjectsOfType<NPCManager>();

        int alertedCount = 0;
        foreach (NPCManager npc in allNPCs)
        {
            float distance = Vector3.Distance(gunshotPosition, npc.transform.position);
            
            if (distance <= range)
            {
                npc.OnGunshotHeard(gunshotPosition);
                alertedCount++;
            }
        }

        Debug.Log($"Gunshot at {gunshotPosition}. Alerted {alertedCount}/{allNPCs.Length} NPCs");
    }

    // Visualization in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gunshotHearingRange);
    }
}
