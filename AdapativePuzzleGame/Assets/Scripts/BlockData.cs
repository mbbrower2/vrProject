using UnityEngine;

[CreateAssetMenu(menuName = "VR Puzzle/Block")]
public class BlockData : ScriptableObject
{
    public string blockName;
    public Vector3Int[] cells; // relative cell offsets
    public Color color = Color.white;
    public float cellSize = 0.1f;
}