using UnityEngine;
using System.Collections.Generic;

public class TemplateSpawner : MonoBehaviour
{
    public TemplateData templateData;
    public GameObject snapSlotPrefab;

    [HideInInspector] public List<SnapSlot> spawnedSlots = new List<SnapSlot>();
    [HideInInspector] public Dictionary<Vector3Int, SnapSlot> slotMap = new Dictionary<Vector3Int, SnapSlot>();

    void Start() => BuildTemplate();

    public void BuildTemplate()
    {
        foreach (var s in spawnedSlots)
            if (s != null) Destroy(s.gameObject);
        spawnedSlots.Clear();
        slotMap.Clear();

        if (templateData == null) { Debug.LogError("No TemplateData assigned!"); return; }
        if (snapSlotPrefab == null) { Debug.LogError("No SnapSlot prefab assigned!"); return; }

        foreach (Vector3Int cell in templateData.slots)
        {
            Vector3 worldPos = transform.position
                + transform.rotation * new Vector3(cell.x, cell.y, cell.z) * templateData.cellSize;

            GameObject slotObj = Instantiate(snapSlotPrefab, worldPos,
                                             transform.rotation, transform);
            slotObj.name = $"Slot_{cell.x}_{cell.y}_{cell.z}";

            SnapSlot slot = slotObj.GetComponent<SnapSlot>();
            slot.gridPos = cell;
            spawnedSlots.Add(slot);
            slotMap[cell] = slot;
        }

        Debug.Log($"Template '{templateData.templateName}' built with {spawnedSlots.Count} slots.");
    }

    public SnapSlot GetSlot(Vector3Int gridPos)
    {
        slotMap.TryGetValue(gridPos, out SnapSlot slot);
        return slot;
    }
}