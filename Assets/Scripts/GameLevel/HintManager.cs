using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BoardGrid))]
public class HintManager : MonoBehaviour
{
    BoardGrid grid;
    Block[] blockPrefabs;
    Block currentHint;

    void Awake() => grid = GetComponent<BoardGrid>();

    void Start()
    {
        blockPrefabs = Resources.LoadAll<Block>("Blocks");
    }

    public void DisplayTestHint()
    {
        DisplayHint(blockPrefabs[1], new(2, 1), BoardGrid.Rotation.Deg0, false, 0);
    }

    public void DisplayHint(Block block, Vector2Int cell, BoardGrid.Rotation rotation, bool flipped, int pivotIndex)
    {
        Debug.Log($"Displaying hint for block {block.name} at cell {cell} with rotation {rotation} and flipped {flipped}");
        HideHint();

        Block prefab = block.Prefab != null ? block.Prefab : block;

        currentHint = Instantiate(prefab, transform);
        currentHint.Initialize(prefab, pivotIndex, Block.Mobility.Fixed);
        currentHint.SetRotation(currentHint.Pivot, rotation);
        if (flipped) currentHint.Flip(currentHint.Pivot);

        Vector3 pivotTarget = grid.TileToWorld(cell);
        Vector3 delta = pivotTarget - currentHint.Pivot.transform.position;
        Vector3 placedPos = currentHint.transform.position + delta;
        currentHint.transform.position = placedPos;
        currentHint.Position2D = placedPos;
        currentHint.DepthOffset = 0.1f;

        ApplyTransparentColor(currentHint, Color.cyan);
    }

    public void HideHint()
    {
        if (currentHint == null) return;
        Destroy(currentHint.gameObject);
        currentHint = null;
    }

    void ApplyTransparentColor(Block block, Color color)
    {
        foreach (Renderer r in block.GetComponentsInChildren<Renderer>())
        {
            Material mat = r.material;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = color;
        }
    }
}
