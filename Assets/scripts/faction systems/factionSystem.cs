using System.Collections.Generic;
using UnityEngine;

public enum Faction
{
    Cruds,
    Tripz,
    HeavensDevils,
    ThePharaohs,
    RoastMaToasta,
    WhiteWater,
    TheDemencoFamily
    // Add more factions as needed
}

public class FactionReputationSystem : MonoBehaviour
{
    public static FactionReputationSystem Instance { get; private set; }

    private Dictionary<Faction, int> factionReputation = new Dictionary<Faction, int>();

    private const int MIN_REPUTATION = -1000;
    private const int MAX_REPUTATION = 1000;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeFactions();
    }

    private void InitializeFactions()
    {
        foreach (Faction faction in System.Enum.GetValues(typeof(Faction)))
        {
            if (!factionReputation.ContainsKey(faction))
                factionReputation[faction] = 0;
        }
    }

    /// <summary>
    /// Add reputation points to a faction (clamped to max 1000)
    /// </summary>
    public void GainReputation(Faction faction, int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Use LoseReputation() for negative reputation changes");
            return;
        }

        int newReputation = factionReputation[faction] + amount;
        newReputation = Mathf.Clamp(newReputation, MIN_REPUTATION, MAX_REPUTATION);

        int actualGain = newReputation - factionReputation[faction];
        factionReputation[faction] = newReputation;

        if (actualGain > 0)
        {
            Debug.Log($"{faction} reputation increased by {actualGain}. Total: {factionReputation[faction]}");
        }
        else if (actualGain == 0 && factionReputation[faction] == MAX_REPUTATION)
        {
            Debug.Log($"{faction} reputation is at maximum ({MAX_REPUTATION})");
        }
    }

    /// <summary>
    /// Lose reputation points from a faction (clamped to min -1000)
    /// </summary>
    public void LoseReputation(Faction faction, int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Use GainReputation() for positive reputation changes");
            return;
        }

        int newReputation = factionReputation[faction] - amount;
        newReputation = Mathf.Clamp(newReputation, MIN_REPUTATION, MAX_REPUTATION);

        int actualLoss = factionReputation[faction] - newReputation;
        factionReputation[faction] = newReputation;

        if (actualLoss > 0)
        {
            Debug.Log($"{faction} reputation decreased by {actualLoss}. Total: {factionReputation[faction]}");
        }
        else if (actualLoss == 0 && factionReputation[faction] == MIN_REPUTATION)
        {
            Debug.Log($"{faction} reputation is at minimum ({MIN_REPUTATION})");
        }
    }

    /// <summary>
    /// Get the current reputation value for a faction
    /// </summary>
    public int GetReputation(Faction faction)
    {
        return factionReputation[faction];
    }

    /// <summary>
    /// Set reputation to a specific value (clamped to -1000 to 1000)
    /// </summary>
    public void SetReputation(Faction faction, int value)
    {
        value = Mathf.Clamp(value, MIN_REPUTATION, MAX_REPUTATION);
        factionReputation[faction] = value;
        Debug.Log($"{faction} reputation set to {value}");
    }

    /// <summary>
    /// Reset all faction reputation to 0
    /// </summary>
    public void ResetAllReputation()
    {
        foreach (var faction in factionReputation.Keys)
        {
            factionReputation[faction] = 0;
        }
        Debug.Log("All faction reputation reset to 0");
    }
}