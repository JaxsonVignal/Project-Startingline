using UnityEngine;
using System;
using System.Collections.Generic;

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

    // ========== REMOVAL MINIGAMES ==========

    /// <summary>
    /// Start a barrel removal minigame (unscrew and remove)
    /// </summary>
    public void StartBarrelRemovalMinigame(
        AttachmentData barrelToRemove,
        WeaponData weapon,
        Transform socket,
        Action<AttachmentData> onComplete)
    {
        Debug.Log($"StartBarrelRemovalMinigame: {barrelToRemove.name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        // Find the barrel GameObject in the scene
        GameObject barrelInScene = FindAttachmentInScene(socket, barrelToRemove);

        if (barrelInScene == null)
        {
            Debug.LogWarning("Could not find barrel in scene - completing without minigame");

            // Still re-enable default barrel parts even if minigame is skipped
            if (weapon != null)
            {
                var partsToReEnable = weapon.GetPartsToDisableWithBarrel();
                if (partsToReEnable != null && partsToReEnable.Count > 0)
                {
                    ReEnableWeaponParts(socket.root, partsToReEnable);
                }
            }

            onComplete?.Invoke(barrelToRemove);
            return;
        }

        // Add the BarrelRemovalMinigame component
        BarrelRemovalMinigame minigame = barrelInScene.AddComponent<BarrelRemovalMinigame>();

        // Get the parts to re-enable when barrel is removed (default barrel parts)
        if (weapon != null)
        {
            var partsToReEnable = weapon.GetPartsToDisableWithBarrel();
            if (partsToReEnable != null && partsToReEnable.Count > 0)
            {
                minigame.SetWeaponPartsToReEnable(socket.root, partsToReEnable);
            }
        }

        minigame.attachmentData = barrelToRemove;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();

        Debug.Log($"Started barrel removal minigame");
    }

    /// <summary>
    /// Start a scope removal minigame (unscrew and remove)
    /// </summary>
    public void StartScopeRemovalMinigame(
        AttachmentData scopeToRemove,
        WeaponData weapon,
        Transform socket,
        Action<AttachmentData> onComplete)
    {
        Debug.Log($"StartScopeRemovalMinigame: {scopeToRemove.name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        GameObject scopeInScene = FindAttachmentInScene(socket, scopeToRemove);

        if (scopeInScene == null)
        {
            Debug.LogWarning("Could not find scope in scene - completing without minigame");

            // Still re-enable iron sights even if minigame is skipped
            if (weapon != null)
            {
                var partsToReEnable = weapon.GetPartsToDisableWithSight();
                if (partsToReEnable != null && partsToReEnable.Count > 0)
                {
                    ReEnableWeaponParts(socket.root, partsToReEnable);
                }
            }

            onComplete?.Invoke(scopeToRemove);
            return;
        }

        ScopeRemovalMinigame minigame = scopeInScene.AddComponent<ScopeRemovalMinigame>();

        // Assign screw prefab
        if (screwPrefab != null)
        {
            var screwPrefabField = typeof(ScopeRemovalMinigame).GetField("screwPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (screwPrefabField != null)
            {
                screwPrefabField.SetValue(minigame, screwPrefab);
            }
        }

        // Get the parts to re-enable when scope is removed (iron sights)
        if (weapon != null)
        {
            var partsToReEnable = weapon.GetPartsToDisableWithSight();
            if (partsToReEnable != null && partsToReEnable.Count > 0)
            {
                minigame.SetWeaponPartsToReEnable(socket.root, partsToReEnable);
            }
        }

        minigame.attachmentData = scopeToRemove;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();

        Debug.Log($"Started scope removal minigame");
    }

    /// <summary>
    /// Start an underbarrel removal minigame (unscrew and remove)
    /// </summary>
    public void StartUnderbarrelRemovalMinigame(
        AttachmentData underbarrelToRemove,
        WeaponData weapon,
        Transform socket,
        Action<AttachmentData> onComplete)
    {
        Debug.Log($"StartUnderbarrelRemovalMinigame: {underbarrelToRemove.name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        GameObject underbarrelInScene = FindAttachmentInScene(socket, underbarrelToRemove);

        if (underbarrelInScene == null)
        {
            Debug.LogWarning("Could not find underbarrel in scene - completing without minigame");
            onComplete?.Invoke(underbarrelToRemove);
            return;
        }

        UnderbarrelRemovalMinigame minigame = underbarrelInScene.AddComponent<UnderbarrelRemovalMinigame>();

        // Assign screw prefab
        if (screwPrefab != null)
        {
            var screwPrefabField = typeof(UnderbarrelRemovalMinigame).GetField("screwPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (screwPrefabField != null)
            {
                screwPrefabField.SetValue(minigame, screwPrefab);
            }
        }

        minigame.attachmentData = underbarrelToRemove;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();

        Debug.Log($"Started underbarrel removal minigame");
    }

    /// <summary>
    /// Start a siderail removal minigame (slide off and remove)
    /// </summary>
    public void StartSiderailRemovalMinigame(
        AttachmentData siderailToRemove,
        WeaponData weapon,
        Transform socket,
        Action<AttachmentData> onComplete)
    {
        Debug.Log($"StartSiderailRemovalMinigame: {siderailToRemove.name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        GameObject siderailInScene = FindAttachmentInScene(socket, siderailToRemove);

        if (siderailInScene == null)
        {
            Debug.LogWarning("Could not find siderail in scene - completing without minigame");
            onComplete?.Invoke(siderailToRemove);
            return;
        }

        SiderailRemovalMinigame minigame = siderailInScene.AddComponent<SiderailRemovalMinigame>();

        minigame.attachmentData = siderailToRemove;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();

        Debug.Log($"Started siderail removal minigame");
    }

    /// <summary>
    /// Start a magazine removal minigame (pull out and remove)
    /// </summary>
    public void StartMagazineRemovalMinigame(
        AttachmentData magazineToRemove,
        WeaponData weapon,
        Transform socket,
        Action<AttachmentData> onComplete)
    {
        Debug.Log($"StartMagazineRemovalMinigame: {magazineToRemove.name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        GameObject magazineInScene = FindAttachmentInScene(socket, magazineToRemove);

        if (magazineInScene == null)
        {
            Debug.LogWarning("Could not find magazine in scene - completing without minigame");

            // Still re-enable default magazine even if minigame is skipped
            if (weapon != null)
            {
                var partsToReEnable = weapon.GetPartsToDisableWithMagazine();
                if (partsToReEnable != null && partsToReEnable.Count > 0)
                {
                    ReEnableWeaponParts(socket.root, partsToReEnable);
                }
            }

            onComplete?.Invoke(magazineToRemove);
            return;
        }

        MagazineRemovalMinigame minigame = magazineInScene.AddComponent<MagazineRemovalMinigame>();

        // Get the parts to re-enable when magazine is removed (default magazine)
        if (weapon != null)
        {
            var partsToReEnable = weapon.GetPartsToDisableWithMagazine();
            if (partsToReEnable != null && partsToReEnable.Count > 0)
            {
                minigame.SetWeaponPartsToReEnable(socket.root, partsToReEnable);
            }
        }

        minigame.attachmentData = magazineToRemove;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();

        Debug.Log($"Started magazine removal minigame");
    }

    // ========== HELPER METHODS ==========

    /// <summary>
    /// Re-enable weapon parts by path (used when minigame is skipped)
    /// </summary>
    private void ReEnableWeaponParts(Transform weaponRoot, List<string> partPaths)
    {
        if (weaponRoot == null || partPaths == null || partPaths.Count == 0)
            return;

        Debug.Log($"Re-enabling {partPaths.Count} weapon part(s)");

        foreach (var partPath in partPaths)
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
                partTransform.gameObject.SetActive(true);
                Debug.Log($"  Re-enabled: {partPath}");
            }
            else
            {
                Debug.LogWarning($"  Could not find part to re-enable: {partPath}");
            }
        }
    }

    private GameObject FindAttachmentInScene(Transform socket, AttachmentData attachment)
    {
        if (socket == null) return null;

        string expectedName = $"ATT_{attachment.id}";

        foreach (Transform child in socket)
        {
            if (child.name == expectedName || child.name.Contains(attachment.prefab.name))
            {
                return child.gameObject;
            }
        }

        Debug.LogWarning($"Could not find attachment with name '{expectedName}' in socket");
        return null;
    }

    // ========== ORIGINAL METHODS (UNCHANGED) ==========

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

            case AttachmentType.SideRail:
                minigame = SetupSiderailMinigame(attachmentObj);
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

        HideDefaultBarrelParts(socket.root, weapon);

        GameObject newBarrelObj = Instantiate(newBarrel.prefab, minigameParent);

        if (newBarrelObj.GetComponent<Collider>() == null)
        {
            newBarrelObj.AddComponent<BoxCollider>();
        }

        SilencerMinigame minigame = newBarrelObj.AddComponent<SilencerMinigame>();

        GameObject oldBarrelInScene = FindOldBarrelInScene(socket, oldBarrel);

        if (oldBarrelInScene != null)
        {
            Debug.Log($"Found old barrel in scene: {oldBarrelInScene.name}");

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

        if (screwPrefab != null)
        {
            var screwPrefabField = typeof(ScopeMinigame).GetField("screwPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (screwPrefabField != null)
            {
                screwPrefabField.SetValue(minigame, screwPrefab);
            }
        }

        GameObject oldScopeInScene = FindOldScopeInScene(socket, oldScope);

        if (oldScopeInScene != null)
        {
            Debug.Log($"Found old scope in scene: {oldScopeInScene.name}");

            var partsToRemove = new System.Collections.Generic.List<string> { oldScopeInScene.name };
            minigame.SetOldScopeParts(socket.root, partsToRemove, oldScope);
        }

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

        GameObject newUnderbarrelObj = Instantiate(newUnderbarrel.prefab, minigameParent);

        if (newUnderbarrelObj.GetComponent<Collider>() == null)
        {
            newUnderbarrelObj.AddComponent<BoxCollider>();
        }

        UnderbarrelMinigame minigame = newUnderbarrelObj.AddComponent<UnderbarrelMinigame>();

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

        GameObject oldUnderbarrelInScene = FindOldUnderbarrelInScene(socket, oldUnderbarrel);

        if (oldUnderbarrelInScene != null)
        {
            Debug.Log($"Found old underbarrel in scene: {oldUnderbarrelInScene.name}");

            var partsToRemove = new System.Collections.Generic.List<string> { oldUnderbarrelInScene.name };

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

    public void StartSiderailReplacementMinigame(
        AttachmentData oldSiderail,
        AttachmentData newSiderail,
        WeaponData weapon,
        Transform socket,
        Action<AttachmentData> onComplete)
    {
        Debug.Log($"StartSiderailReplacementMinigame: {oldSiderail.name} -> {newSiderail.name}");

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
        }

        onMinigameCompleteCallback = onComplete;

        GameObject newSiderailObj = Instantiate(newSiderail.prefab, minigameParent);

        if (newSiderailObj.GetComponent<Collider>() == null)
        {
            newSiderailObj.AddComponent<BoxCollider>();
        }

        SiderailMinigame minigame = newSiderailObj.AddComponent<SiderailMinigame>();

        GameObject oldSiderailInScene = FindOldSiderailInScene(socket, oldSiderail);

        if (oldSiderailInScene != null)
        {
            Debug.Log($"Found old siderail in scene: {oldSiderailInScene.name}");

            var partsToRemove = new System.Collections.Generic.List<string> { oldSiderailInScene.name };

            minigame.SetOldSiderailParts(socket.root, partsToRemove);
        }
        else
        {
            Debug.LogWarning("Could not find old siderail in scene - will skip removal phase");
        }

        minigame.attachmentData = newSiderail;
        minigame.targetSocket = socket;
        minigame.weaponData = weapon;
        minigame.minigameCamera = previewCamera;

        minigame.OnMinigameComplete += OnMinigameCompleted;
        minigame.OnMinigameCancelled += OnMinigameCancelled;

        currentMinigame = minigame;
        minigame.StartMinigame();

        Debug.Log($"Started siderail replacement minigame");
    }

    private GameObject FindOldSiderailInScene(Transform socket, AttachmentData oldSiderail)
    {
        if (socket == null) return null;

        string expectedName = $"ATT_{oldSiderail.id}";

        foreach (Transform child in socket)
        {
            if (child.name == expectedName || child.name.Contains(oldSiderail.prefab.name))
            {
                return child.gameObject;
            }
        }

        Debug.LogWarning($"Could not find old siderail with name '{expectedName}' in socket");
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

    private SiderailMinigame SetupSiderailMinigame(GameObject attachmentObj)
    {
        SiderailMinigame siderailMinigame = attachmentObj.AddComponent<SiderailMinigame>();
        return siderailMinigame;
    }

    private bool HasMinigameImplementation(AttachmentType type)
    {
        switch (type)
        {
            case AttachmentType.Barrel:
            case AttachmentType.Sight:
            case AttachmentType.Underbarrel:
            case AttachmentType.Magazine:
            case AttachmentType.SideRail:
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

        if (minigameParent != null)
        {
            Debug.Log($"Destroying all children of minigameParent ({minigameParent.childCount} children)");

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

            var minigameToCancel = currentMinigame;

            currentMinigame = null;
            onMinigameCompleteCallback = null;

            minigameToCancel.CancelMinigame();
        }

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