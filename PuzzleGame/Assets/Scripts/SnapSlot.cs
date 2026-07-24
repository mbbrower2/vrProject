using UnityEngine;

public class SnapSlot : MonoBehaviour
{
    [HideInInspector] public Vector3Int gridPos;
    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public BlockSpawner occupant;

    private MeshRenderer meshRenderer;
    private Color defaultColor;
    private Color highlightColor = new Color(0f, 1f, 0f, 0.25f);

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        defaultColor = meshRenderer.material.color;
    }

    public void SetHighlight(bool on)
    {
        if (meshRenderer != null)
            meshRenderer.material.color = on ? highlightColor : defaultColor;
    }

    public void SetOccupied(BlockSpawner block)
    {
        isOccupied = true;
        occupant = block;
        if (meshRenderer != null)
            meshRenderer.enabled = false;
    }

    public void ShowSlot()
    {
        isOccupied = false;
        occupant = null;
        if (meshRenderer != null)
            meshRenderer.enabled = true;
        SetHighlight(false);
    }
}