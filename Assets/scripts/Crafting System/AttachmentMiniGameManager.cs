using UnityEngine;
using System;

public class AttachmentMinigameManager : MonoBehaviour
{
    [Header("Minigame Prefabs")]
    [SerializeField] private GameObject silencerMinigamePrefab;

    [Header("Scope Minigame Settings")]
    [SerializeField] private GameObject screwPrefab;

    [Header("References")]
    [SerializeField] private Transform minigameParent;
    [SerializeField] private Camera previewCamera;

    private AttachmentMinigameBase currentMinigame;
    private Action<AttachmentData> onMinigameCompleteCallback;

    public void StartMinigame(AttachmentData attachment, WeaponData weapon, Transform socket, Action<AttachmentData> onComplete)
    {
        Debug.Log($"AttachmentMinigameManager.StartMinigame called for {attachment.Name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        if (!HasMinigameImplementation(attachment.type))
        {
            Debug.LogWarning($"No minigame implementation for {attachment.type}. Adding attachment directly.");
            onComplete?.Invoke(attachment);
            return;
        }

        GameObject attachmentObj = Instantiate(attachment.prefab, minigameParent);

        if (attachmentObj.GetComponent<Collider>() == null)
        {
            attachmentObj.AddComponent<BoxCollider>();
        }

        AttachmentMinigameBase minigame = null;

        switch (attachment.type)
        {
            case AttachmentType.Barrel:
                minigame = SetupSilencerMinigame(attachmentObj, weapon, socket);
                break;

            case AttachmentType.Sight:
                minigame = SetupScopeMinigame(attachmentObj, weapon, socket);
                break;

            case AttachmentType.Underbarrel:
                minigame = SetupUnderbarrelMinigame(attachmentObj);
                break;

            case AttachmentType.Magazine:
                minigame = SetupMagazineMinigame(attachmentObj, weapon, socket);
                break;

            default:
                Debug.LogWarning($"No minigame implementation for {attachment.type}");
                onComplete?.Invoke(attachment);
                Destroy(attachmentObj);
                return;
        }

        minigame.attachmentData = attachment;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();
    }

    /// <summary>
    /// Start a barrel replacement minigame (unscrew old, screw in new)
    /// </summary>
    public void StartBarrelReplacementMinigame(
        AttachmentData oldBarrel,
        AttachmentData newBarrel,
        WeaponData weapon,
        Transform socket,
        Action<AttachmentData> onComplete)
    {
        Debug.Log($"StartBarrelReplacementMinigame: {oldBarrel.name} -> {newBarrel.name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        // Find and HIDE the default barrel parts IMMEDIATELY before starting the minigame
        HideDefaultBarrelParts(socket.root, weapon);

        // Spawn the NEW barrel attachment prefab
        GameObject newBarrelObj = Instantiate(newBarrel.prefab, minigameParent);

        if (newBarrelObj.GetComponent<Collider>() == null)
        {
            newBarrelObj.AddComponent<BoxCollider>();
        }

        SilencerMinigame minigame = newBarrelObj.AddComponent<SilencerMinigame>();

        // Find the old barrel GameObject in the scene to remove it
        GameObject oldBarrelInScene = FindOldBarrelInScene(socket, oldBarrel);

        if (oldBarrelInScene != null)
        {
            Debug.Log($"Found old barrel in scene: {oldBarrelInScene.name}");

            // Create a temporary list with just the old barrel's name
            var partsToRemove = new System.Collections.Generic.List<string> { oldBarrelInScene.name };

            minigame.SetWeaponPartsToDisable(socket.root, partsToRemove);
        }
        else
        {
            Debug.LogWarning("Could not find old barrel in scene - will skip removal phase");
        }

        minigame.attachmentData = newBarrel;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();

        Debug.Log($"Started barrel replacement minigame");
    }

    /// <summary>
    /// Hide the default barrel parts during barrel replacement
    /// </summary>
    private void HideDefaultBarrelParts(Transform weaponRoot, WeaponData weaponData)
    {
        if (weaponRoot == null || weaponData == null) return;

        var partsToHide = weaponData.GetPartsToDisableWithBarrel();

        if (partsToHide == null || partsToHide.Count == 0)
        {
            Debug.Log("No default barrel parts to hide");
            return;
        }

        Debug.Log($"Hiding {partsToHide.Count} default barrel parts during replacement");

        foreach (var partPath in partsToHide)
        {
            if (string.IsNullOrEmpty(partPath))
                continue;

            Transform partTransform = weaponRoot.Find(partPath);

            if (partTransform == null)
            {
                partTransform = FindChildByNameRecursive(weaponRoot, partPath);
            }

            if (partTransform != null)
            {
                partTransform.gameObject.SetActive(false);
                Debug.Log($"Hid default barrel part: {partPath}");
            }
            else
            {
                Debug.LogWarning($"Could not find default barrel part to hide: {partPath}");
            }
        }
    }

    /// <summary>
    /// Recursive helper to find child transforms
    /// </summary>
    private Transform FindChildByNameRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform result = FindChildByNameRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private GameObject FindOldBarrelInScene(Transform socket, AttachmentData oldBarrel)
    {
        if (socket == null) return null;

        string expectedName = $"ATT_{oldBarrel.id}";

        foreach (Transform child in socket)
        {
            if (child.name == expectedName || child.name.Contains(oldBarrel.prefab.name))
            {
                return child.gameObject;
            }
        }

        Debug.LogWarning($"Could not find old barrel with name '{expectedName}' in socket");
        return null;
    }

    /// <summary>
    /// Start a scope replacement minigame (unscrew old, screw in new)
    /// </summary>
    public void StartScopeReplacementMinigame(
        AttachmentData oldScope,
        AttachmentData newScope,
        WeaponData weapon,
        Transform socket,
        Action<AttachmentData> onComplete)
    {
        Debug.Log($"StartScopeReplacementMinigame: {oldScope.name} -> {newScope.name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        GameObject newScopeObj = Instantiate(newScope.prefab, minigameParent);

        if (newScopeObj.GetComponent<Collider>() == null)
        {
            newScopeObj.AddComponent<BoxCollider>();
        }

        ScopeMinigame minigame = newScopeObj.AddComponent<ScopeMinigame>();

        // Assign screw prefab
        if (screwPrefab != null)
        {
            var screwPrefabField = typeof(ScopeMinigame).GetField("screwPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (screwPrefabField != null)
            {
                screwPrefabField.SetValue(minigame, screwPrefab);
            }
        }

        // Find old scope in scene
        GameObject oldScopeInScene = FindOldScopeInScene(socket, oldScope);

        if (oldScopeInScene != null)
        {
            Debug.Log($"Found old scope in scene: {oldScopeInScene.name}");

            var partsToRemove = new System.Collections.Generic.List<string> { oldScopeInScene.name };
            minigame.SetOldScopeParts(socket.root, partsToRemove, oldScope);
        }

        // Set weapon parts to disable (iron sights)
        if (weapon != null)
        {
            var partsToDisable = weapon.GetPartsToDisableWithSight();
            if (partsToDisable != null && partsToDisable.Count > 0)
            {
                minigame.SetWeaponPartsToDisable(socket.root, partsToDisable);
            }
        }

        minigame.attachmentData = newScope;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();
    }

    private GameObject FindOldScopeInScene(Transform socket, AttachmentData oldScope)
    {
        if (socket == null) return null;

        string expectedName = $"ATT_{oldScope.id}";

        foreach (Transform child in socket)
        {
            if (child.name == expectedName || child.name.Contains(oldScope.prefab.name))
            {
                return child.gameObject;
            }
        }

        Debug.LogWarning($"Could not find old scope with name '{expectedName}' in socket");
        return null;
    }

    /// <summary>
    /// Start an underbarrel replacement minigame (unscrew old, screw in new)
    /// </summary>
    public void StartUnderbarrelReplacementMinigame(
        AttachmentData oldUnderbarrel,
        AttachmentData newUnderbarrel,
        WeaponData weapon,
        Transform socket,
        Action<AttachmentData> onComplete)
    {
        Debug.Log($"StartUnderbarrelReplacementMinigame: {oldUnderbarrel.name} -> {newUnderbarrel.name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        // Spawn the NEW underbarrel attachment prefab
        GameObject newUnderbarrelObj = Instantiate(newUnderbarrel.prefab, minigameParent);

        if (newUnderbarrelObj.GetComponent<Collider>() == null)
        {
            newUnderbarrelObj.AddComponent<BoxCollider>();
        }

        UnderbarrelMinigame minigame = newUnderbarrelObj.AddComponent<UnderbarrelMinigame>();

        // Assign screw prefab
        if (screwPrefab != null)
        {
            var screwPrefabField = typeof(UnderbarrelMinigame).GetField("screwPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (screwPrefabField != null)
            {
                screwPrefabField.SetValue(minigame, screwPrefab);
                Debug.Log($"Assigned screw prefab to UnderbarrelMinigame: {screwPrefab.name}");
            }
        }

        // Find the old underbarrel GameObject in the scene to remove it
        GameObject oldUnderbarrelInScene = FindOldUnderbarrelInScene(socket, oldUnderbarrel);

        if (oldUnderbarrelInScene != null)
        {
            Debug.Log($"Found old underbarrel in scene: {oldUnderbarrelInScene.name}");

            // Create a list with just the old underbarrel's name
            var partsToRemove = new System.Collections.Generic.List<string> { oldUnderbarrelInScene.name };

            // IMPORTANT: Pass the OLD attachment data so it uses the correct screw positions
            minigame.SetOldUnderbarrelParts(socket.root, partsToRemove, oldUnderbarrel);
        }
        else
        {
            Debug.LogWarning("Could not find old underbarrel in scene - will skip removal phase");
        }

        minigame.attachmentData = newUnderbarrel;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();

        Debug.Log($"Started underbarrel replacement minigame");
    }

    private GameObject FindOldUnderbarrelInScene(Transform socket, AttachmentData oldUnderbarrel)
    {
        if (socket == null) return null;

        string expectedName = $"ATT_{oldUnderbarrel.id}";

        foreach (Transform child in socket)
        {
            if (child.name == expectedName || child.name.Contains(oldUnderbarrel.prefab.name))
            {
                return child.gameObject;
            }
        }

        Debug.LogWarning($"Could not find old underbarrel with name '{expectedName}' in socket");
        return null;
    }

    private SilencerMinigame SetupSilencerMinigame(GameObject attachmentObj, WeaponData weapon, Transform socket)
    {
        SilencerMinigame silencerMinigame = attachmentObj.AddComponent<SilencerMinigame>();

        if (silencerMinigame != null && weapon != null && socket != null)
        {
            var partsToDisable = weapon.GetPartsToDisableWithBarrel();
            if (partsToDisable != null && partsToDisable.Count > 0)
            {
                silencerMinigame.SetWeaponPartsToDisable(socket.root, partsToDisable);
            }
        }

        return silencerMinigame;
    }

    private ScopeMinigame SetupScopeMinigame(GameObject attachmentObj, WeaponData weapon, Transform socket)
    {
        ScopeMinigame scopeMinigame = attachmentObj.AddComponent<ScopeMinigame>();

        if (screwPrefab != null)
        {
            var screwPrefabField = typeof(ScopeMinigame).GetField("screwPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (screwPrefabField != null)
            {
                screwPrefabField.SetValue(scopeMinigame, screwPrefab);
            }
        }

        if (scopeMinigame != null && weapon != null && socket != null)
        {
            var partsToDisable = weapon.GetPartsToDisableWithSight();
            if (partsToDisable != null && partsToDisable.Count > 0)
            {
                scopeMinigame.SetWeaponPartsToDisable(socket.root, partsToDisable);
            }
        }

        return scopeMinigame;
    }

    private UnderbarrelMinigame SetupUnderbarrelMinigame(GameObject attachmentObj)
    {
        UnderbarrelMinigame underbarrelMinigame = attachmentObj.AddComponent<UnderbarrelMinigame>();

        if (screwPrefab != null)
        {
            var screwPrefabField = typeof(UnderbarrelMinigame).GetField("screwPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (screwPrefabField != null)
            {
                screwPrefabField.SetValue(underbarrelMinigame, screwPrefab);
            }
        }

        return underbarrelMinigame;
    }

    private MagazineMinigame SetupMagazineMinigame(GameObject attachmentObj, WeaponData weapon, Transform socket)
    {
        MagazineMinigame magazineMinigame = attachmentObj.AddComponent<MagazineMinigame>();

        if (magazineMinigame != null && weapon != null && socket != null)
        {
            var partsToDisable = weapon.GetPartsToDisableWithMagazine();
            if (partsToDisable != null && partsToDisable.Count > 0)
            {
                magazineMinigame.SetOldMagazineParts(socket.root, partsToDisable);
            }
        }

        return magazineMinigame;
    }

    private bool HasMinigameImplementation(AttachmentType type)
    {
        switch (type)
        {
            case AttachmentType.Barrel:
            case AttachmentType.Sight:
            case AttachmentType.Underbarrel:
            case AttachmentType.Magazine:
                return true;
            default:
                return false;
        }
    }

    private void OnMinigameCompleted(AttachmentData attachment)
    {
        Debug.Log($"Minigame completed for {attachment.Name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
            currentMinigame = null;
        }

        onMinigameCompleteCallback?.Invoke(attachment);
        onMinigameCompleteCallback = null;
    }

    private void OnMinigameCancelled()
    {
        Debug.Log("Minigame cancelled - cleaning up all minigame objects");

        // UPDATED: Clean up ALL children in the minigame parent
        if (minigameParent != null)
        {
            Debug.Log($"Destroying all children of minigameParent ({minigameParent.childCount} children)");

            // Create a list to avoid modifying collection while iterating
            var childrenToDestroy = new System.Collections.Generic.List<GameObject>();

            foreach (Transform child in minigameParent)
            {
                childrenToDestroy.Add(child.gameObject);
                Debug.Log($"Marking for destruction: {child.gameObject.name}");
            }

            // Destroy all children
            foreach (var child in childrenToDestroy)
            {
                Destroy(child);
            }
        }

        currentMinigame = null;
        onMinigameCompleteCallback = null;
    }

    public bool IsMinigameActive()
    {
        return currentMinigame != null;
    }

    public void CancelCurrentMinigame()
    {
        if (currentMinigame != null)
        {
            Debug.Log($"CancelCurrentMinigame called - cancelling {currentMinigame.gameObject.name}");

            // Store reference before clearing
            var minigameToCancel = currentMinigame;

            // Clear our reference first
            currentMinigame = null;
            onMinigameCompleteCallback = null;

            // Now call CancelMinigame on the stored reference
            // This will trigger cleanup (camera reset) and destroy the GameObject
            minigameToCancel.CancelMinigame();
        }

        // ADDED: Also clean up ALL children in minigame parent as a safety measure
        if (minigameParent != null)
        {
            Debug.Log($"CancelCurrentMinigame - cleaning up minigameParent ({minigameParent.childCount} children)");

            var childrenToDestroy = new System.Collections.Generic.List<GameObject>();

            foreach (Transform child in minigameParent)
            {
                childrenToDestroy.Add(child.gameObject);
                Debug.Log($"Marking for destruction: {child.gameObject.name}");
            }

            foreach (var child in childrenToDestroy)
            {
                Destroy(child);
            }
        }
    }
}