using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoardGrid))]
public class SlotManager : MonoBehaviour
{
    [field: SerializeField] public float HorizontalMargin { get; set; } = 3f;
    [field: SerializeField] public float TotalVerticalSpace { get; set; } = 5f;

    class Slot
    {
        public Vector3 Position { get; set; }
        public Block Occupant { get; set; }
        public Slot(Vector3 position) => Position = position;
    }

    BoardGrid board;
    readonly List<Slot> slots = new();

    void Awake() => board = GetComponent<BoardGrid>();
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.limeGreen;
        foreach (var slot in slots) Gizmos.DrawSphere(slot.Position, 0.05f);
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

        for (int i = 0; i < leftCount; i++) slots.Add(new(new Vector3(leftX, CalculateY(i, leftCount, center.y))));
        for (int i = 0; i < rightCount; i++) slots.Add(new(new Vector3(rightX, CalculateY(i, rightCount, center.y))));
    }

    public void AsignAvailableSlot(Block block)
    {
        foreach (Slot slot in slots)
        {
            if (slot.Occupant != null) continue;

            slot.Occupant = block;
            block.transform.position = slot.Position;
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