using System.Collections.Generic;
using UnityEngine;

public class SlotManager : MonoBehaviour
{
    [field: SerializeField] public float HorizontalMargin { get; set; } = 3f;
    [field: SerializeField] public float TotalVerticalSpace { get; set; } = 5f;

    class Slot
    {
        public Vector2 Position { get; set; }
        public Block Occupant { get; set; }
        public Slot(Vector2 position) => Position = position;
    }

    readonly List<Slot> slots = new();

    void OnDrawGizmos()
    {
        Gizmos.color = Color.limeGreen;
        foreach (var slot in slots) Gizmos.DrawSphere(slot.Position, 0.05f);
    }

    public void Initialize(int totalBlocks)
    {
        slots.Clear();

        if (totalBlocks <= 0) return;

        BoardGrid grid = LevelManager.Current.Grid;
        int leftCount = Mathf.CeilToInt(totalBlocks / 2f);
        int rightCount = totalBlocks / 2;

        float gridRealWidth = grid.Size.x * grid.TileSize;
        float gridRealHeight = grid.Size.y * grid.TileSize;
        Vector2 center = new(gridRealWidth / 2f - (grid.TileSize / 2f), gridRealHeight / 2f - (grid.TileSize / 2f));

        float leftX = center.x - (gridRealWidth / 2f) - HorizontalMargin;
        float rightX = center.x + (gridRealWidth / 2f) + HorizontalMargin;

        for (int i = 0; i < leftCount; i++) slots.Add(new(new Vector2(leftX, CalculateY(i, leftCount, center.y))));
        for (int i = 0; i < rightCount; i++) slots.Add(new(new Vector2(rightX, CalculateY(i, rightCount, center.y))));
    }

    public void AsignAvailableSlot(Block block)
    {
        foreach (Slot slot in slots)
        {
            if (slot.Occupant != null) continue;

            slot.Occupant = block;
            block.Position2D = slot.Position;
            return;
        }
        Debug.LogWarning("No available slots to assign.");
    }

    public void FreeSlot(Block block)
    {
        foreach (var slot in slots)
        {
            if (slot.Occupant == block)
            {
                slot.Occupant = null;
                return;
            }
        }
    }

    float CalculateY(int index, int totalInSide, float centerY) =>
        totalInSide <= 1 ? centerY : centerY + (TotalVerticalSpace / 2f) - (index * (TotalVerticalSpace / (totalInSide - 1)));
}