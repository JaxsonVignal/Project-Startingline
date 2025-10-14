using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(AttachmentSlotMap))]
public class WeaponAttachmentSystem : MonoBehaviour
{
    public WeaponData weaponData;
    public List<AttachmentData> equippedAttachments = new List<AttachmentData>();
    private Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();

    public float currentDamage { get; private set; }
    public float currentFireRate { get; private set; }

    private AttachmentSlotMap slotMap;

    void Awake()
    {
        slotMap = GetComponent<AttachmentSlotMap>();
    }

    void Start()
    {
        RecalculateStats();
    }

    public void EquipAttachment(AttachmentData att)
    {
        if (att == null) return;

        var removed = equippedAttachments.Where(a => a.type == att.type).ToList();
        foreach (var r in removed) DetachById(r.id);

        equippedAttachments.Add(att);

        if (att.prefab != null)
        {
            Transform socket = slotMap.GetSocket(att.type);
            var go = Instantiate(att.prefab, socket ? socket : transform);
            go.name = $"ATT_{att.id}";
            go.transform.localPosition = att.localPosition;
            go.transform.localRotation = Quaternion.Euler(att.localEuler);
            go.transform.localScale = att.localScale;
            spawned[att.id] = go;
        }

        RecalculateStats();
    }

    public void DetachById(string attId)
    {
        var att = equippedAttachments.FirstOrDefault(a => a.id == attId);
        if (att != null) equippedAttachments.Remove(att);

        if (spawned.TryGetValue(attId, out var go))
        {
            Destroy(go);
            spawned.Remove(attId);
        }

        RecalculateStats();
    }

    public void UnequipType(AttachmentType type)
    {
        var list = equippedAttachments.Where(a => a.type == type).ToList();
        foreach (var a in list) DetachById(a.id);
    }

    public void ClearAll()
    {
        foreach (var kv in spawned) if (kv.Value != null) Destroy(kv.Value);
        spawned.Clear();
        equippedAttachments.Clear();
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        currentDamage = weaponData.damage;
        currentFireRate = weaponData.fireRate;
    }
}
