using UnityEngine;
using Oculus.Interaction;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    [HideInInspector] public BlockData blockData;
    public float snapThreshold = 0.1f;

    public static BlockSpawner CurrentlyHeld { get; private set; }

    public Transform VisualWrapper { get; private set; }

    private Grabbable grabbable;
    private Rigidbody rb;
    private TemplateSpawner templateSpawner;
    private List<SnapSlot> currentSlots = new List<SnapSlot>();
    private List<SnapSlot> previewSlots = new List<SnapSlot>();
    private bool isSnapped = false;
    private bool fullyMatched = false;
    private bool isHeld = false;
    public bool IsSnapped => isSnapped;
    public bool FullyMatched => fullyMatched;

    public void SetVisualWrapper(Transform wrapper)
    {
        VisualWrapper = wrapper;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    void Start()
    {
        templateSpawner = FindAnyObjectByType<TemplateSpawner>();
    }

    void OnDestroy()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= OnPointerEvent;
        if (CurrentlyHeld == this)
            CurrentlyHeld = null;
    }

    void OnPointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            isHeld = true;
            CurrentlyHeld = this;
            Unsnap();
        }

        if (evt.Type == PointerEventType.Unselect)
        {
            isHeld = false;
            if (CurrentlyHeld == this)
                CurrentlyHeld = null;

            BakeVisualRotation();

            ClearPreview();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            TrySnap();
        }
    }

    void BakeVisualRotation()
    {
        if (VisualWrapper == null) return;
        transform.rotation = VisualWrapper.rotation;

        VisualWrapper.rotation = transform.rotation;
        VisualWrapper.localRotation = Quaternion.identity;
    }

    void Update()
    {
        if (!isHeld || isSnapped) return;
        UpdatePreview();
    }

    Quaternion TemplateRotation => templateSpawner != null
        ? templateSpawner.transform.rotation
        : Quaternion.identity;

    Vector3Int CellInTemplateSpace(Vector3Int cell, Quaternion blockRotation)
    {
        Vector3 worldOffset = blockRotation *
            (new Vector3(cell.x, cell.y, cell.z) * blockData.cellSize);
        Vector3 templateLocal = Quaternion.Inverse(TemplateRotation) * worldOffset;
        return new Vector3Int(
            Mathf.RoundToInt(templateLocal.x / blockData.cellSize),
            Mathf.RoundToInt(templateLocal.y / blockData.cellSize),
            Mathf.RoundToInt(templateLocal.z / blockData.cellSize));
    }

    Vector3 CellWorldPos(Vector3Int cell)
    {
        Quaternion rot = (VisualWrapper != null && isHeld)
            ? VisualWrapper.rotation
            : transform.rotation;
        return transform.position +
            rot * (new Vector3(cell.x, cell.y, cell.z) * blockData.cellSize);
    }

    bool FindClosestSlot(Quaternion snappedRotation, out SnapSlot closestSlot, out Vector3Int closestCellTemplate)
    {
        closestSlot = null;
        closestCellTemplate = Vector3Int.zero;
        float closestDist = snapThreshold;

        if (templateSpawner == null || blockData == null) return false;

        foreach (Vector3Int cell in blockData.cells)
        {
            Vector3 cellWorld = CellWorldPos(cell);
            Vector3Int cellTemplate = CellInTemplateSpace(cell, snappedRotation);

            foreach (SnapSlot slot in templateSpawner.spawnedSlots)
            {
                if (slot.isOccupied) continue;
                float dist = Vector3.Distance(cellWorld, slot.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestSlot = slot;
                    closestCellTemplate = cellTemplate;
                }
            }
        }

        return closestSlot != null;
    }

    void UpdatePreview()
    {
        ClearPreview();
        if (templateSpawner == null || blockData == null) return;

        foreach (Vector3Int cell in blockData.cells)
        {
            Vector3 cellWorld = CellWorldPos(cell);
            SnapSlot nearest = null;
            float nearestDist = snapThreshold;

            foreach (SnapSlot slot in templateSpawner.spawnedSlots)
            {
                if (slot.isOccupied) continue;
                float dist = Vector3.Distance(cellWorld, slot.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = slot;
                }
            }

            if (nearest != null && !previewSlots.Contains(nearest))
            {
                nearest.SetHighlight(true);
                previewSlots.Add(nearest);
            }
        }
    }

    void ClearPreview()
    {
        foreach (SnapSlot slot in previewSlots)
            if (slot != null && !slot.isOccupied) slot.SetHighlight(false);
        previewSlots.Clear();
    }

    void TrySnap()
    {
        if (isSnapped || templateSpawner == null) return;

        Quaternion templateRot = templateSpawner.transform.rotation;
        Quaternion blockRelativeToTemplate = Quaternion.Inverse(templateRot) * transform.rotation;
        Quaternion snappedRelative = CubeRotations.SnapToNearest(blockRelativeToTemplate);
        Quaternion snappedRotation = templateRot * snappedRelative;

        if (!FindClosestSlot(snappedRotation, out SnapSlot anchorSlot, out Vector3Int anchorCellTemplate))
            return;

        Vector3Int baseGrid = anchorSlot.gridPos - anchorCellTemplate;
        List<SnapSlot> slotsToOccupy = new List<SnapSlot>();

        foreach (Vector3Int cell in blockData.cells)
        {
            Vector3Int cellTemplate = CellInTemplateSpace(cell, snappedRotation);
            Vector3Int targetGrid = baseGrid + cellTemplate;
            SnapSlot targetSlot = templateSpawner.GetSlot(targetGrid);

            if (targetSlot != null && targetSlot.isOccupied)
                return;

            if (targetSlot != null)
                slotsToOccupy.Add(targetSlot);
        }

        fullyMatched = slotsToOccupy.Count == blockData.cells.Length;

        Vector3Int anchorCellRaw = blockData.cells[0];
        foreach (Vector3Int cell in blockData.cells)
        {
            if (CellInTemplateSpace(cell, snappedRotation) == anchorCellTemplate)
            {
                anchorCellRaw = cell;
                break;
            }
        }

        Vector3 anchorLocalOffset = snappedRotation *
            (new Vector3(anchorCellRaw.x, anchorCellRaw.y, anchorCellRaw.z) * blockData.cellSize);

        transform.rotation = snappedRotation;
        transform.position = anchorSlot.transform.position - anchorLocalOffset;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (SnapSlot slot in slotsToOccupy)
            slot.SetOccupied(this);

        currentSlots = slotsToOccupy;
        isSnapped = true;
        transform.SetParent(templateSpawner.transform);

        FindAnyObjectByType<TemplateValidator>()?.CheckCompletion();
    }

    void Unsnap()
    {
        if (!isSnapped) return;

        transform.SetParent(null);

        foreach (SnapSlot slot in currentSlots)
            if (slot != null) slot.ShowSlot();

        currentSlots.Clear();
        isSnapped = false;
        fullyMatched = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
        }
    }
}