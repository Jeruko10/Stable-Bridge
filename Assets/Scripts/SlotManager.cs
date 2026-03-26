using System.Collections.Generic;
using UnityEngine;

public class SlotManager : MonoBehaviour
{
    [field: SerializeField] public BoardGrid Board { get; private set; }
    [field: SerializeField] public float HorizontalMargin { get; set; } = 3f;
    [field: SerializeField] public float TotalVerticalSpace { get; set; } = 8f;

    readonly Dictionary<Vector3, Block> slots = new();

    public void GenerateSlots(int totalBlocks)
    {
        slots.Clear();

        if (totalBlocks <= 0) return;

        int leftCount = Mathf.CeilToInt(totalBlocks / 2f);
        int rightCount = totalBlocks / 2;

        float gridRealWidth = Board.Width * Board.TileSize;
        float gridRealHeight = Board.Height * Board.TileSize;
        Vector3 center = new(gridRealWidth / 2f - (Board.TileSize / 2f), gridRealHeight / 2f - (Board.TileSize / 2f));

        float leftX = center.x - (gridRealWidth / 2f) - HorizontalMargin;
        float rightX = center.x + (gridRealWidth / 2f) + HorizontalMargin;

        for (int i = 0; i < leftCount; i++) slots.Add(new Vector3(leftX, CalculateY(i, leftCount, center.y)), null);
        for (int i = 0; i < rightCount; i++) slots.Add(new Vector3(rightX, CalculateY(i, rightCount, center.y)), null);
    }

    public Vector3? GetAvailableSlot()
    {
        foreach (var pair in slots)
        {
            if (pair.Value == null) return pair.Key;
        }
        return null;
    }

    float CalculateY(int index, int totalInSide, float centerY) =>
        totalInSide <= 1 ? centerY : centerY + (TotalVerticalSpace / 2f) - (index * (TotalVerticalSpace / (totalInSide - 1)));
}