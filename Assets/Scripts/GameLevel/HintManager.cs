using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BoardGrid))]
public class HintManager : MonoBehaviour
{
    [SerializeField] float hintAlpha = 0.4f;

    BoardGrid grid;
    Block currentHint;

    void Awake() => grid = GetComponent<BoardGrid>();

    public void DisplayHint(Block block, Vector2Int cell)
    {
        HideHint();

        if (block.Prefab == null) return;

        currentHint = Instantiate(block.Prefab, transform);
        currentHint.Initialize(block.Prefab, 0, Block.Mobility.Fixed);
        currentHint.SetRotation(currentHint.Pivot, block.Rotation);
        if (block.IsFlipped) currentHint.Flip(currentHint.Pivot);

        Vector3 pivotTarget = grid.TileToWorld(cell);
        Vector3 delta = pivotTarget - currentHint.Pivot.transform.position;
        Vector3 placedPos = currentHint.transform.position + delta;
        currentHint.transform.position = placedPos;
        currentHint.Position2D = placedPos;

        Color color = block.Color;
        color.a = hintAlpha;
        ApplyTransparentColor(currentHint, color);
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
