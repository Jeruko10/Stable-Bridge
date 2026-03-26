using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoardGrid))]
public class SlotManager : MonoBehaviour
{
    [field: SerializeField] public float HorizontalMargin { get; set; } = 3f;
    [field: SerializeField] public float TotalVerticalSpace { get; set; } = 5f;

    BoardGrid board;
    readonly Dictionary<Vector3, Block> slots = new();

    void Awake()
    {
        board = GetComponent<BoardGrid>();
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.limeGreen;
        foreach (var pos in slots.Keys) Gizmos.DrawSphere(pos, 0.05f);
    }

    public void GenerateSlots(int totalBlocks)
    {
        slots.Clear();

        if (totalBlocks <= 0) return;

        int leftCount = Mathf.CeilToInt(totalBlocks / 2f);
        int rightCount = totalBlocks / 2;

        float gridRealWidth = board.Size.x * board.TileSize;
        float gridRealHeight = board.Size.y * board.TileSize;
        Vector3 center = new(gridRealWidth / 2f - (board.TileSize / 2f), gridRealHeight / 2f - (board.TileSize / 2f));

        float leftX = center.x - (gridRealWidth / 2f) - HorizontalMargin;
        float rightX = center.x + (gridRealWidth / 2f) + HorizontalMargin;

        for (int i = 0; i < leftCount; i++) slots.Add(new(leftX, CalculateY(i, leftCount, center.y)), null);
        for (int i = 0; i < rightCount; i++) slots.Add(new(rightX, CalculateY(i, rightCount, center.y)), null);
    }

    public Vector3? GetAvailableSlot()
    {
        foreach (var pair in slots) if (pair.Value == null) return pair.Key;
        return null;
    }

    float CalculateY(int index, int totalInSide, float centerY) =>
        totalInSide <= 1 ? centerY : centerY + (TotalVerticalSpace / 2f) - (index * (TotalVerticalSpace / (totalInSide - 1)));
}