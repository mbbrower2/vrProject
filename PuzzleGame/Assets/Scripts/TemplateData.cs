using UnityEngine;

[CreateAssetMenu(menuName = "VR Puzzle/Template")]
public class TemplateData : ScriptableObject
{
    public string templateName;
    public float cellSize = 0.1f;
    public Vector3Int[] slots;
}