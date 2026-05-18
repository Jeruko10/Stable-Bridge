using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class BlockInventory : MonoBehaviour
{
    [SerializeField] GameObject blockSlotPrefab;
    [SerializeField] PlayerInput playerInput;

    static readonly Vector2 HiddenPosition = new(0f, -9999f);

    readonly List<(Block block, BlockSlotUI slot)> entries = new();
    ScrollRect scrollRect;

    public void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }
    
    public void Clear()
    {
        foreach (var (block, slot) in entries)
            if (slot != null) Destroy(slot.gameObject);
        entries.Clear();
    }

    public void AddBlock(Block block)
    {
        block.Position2D = HiddenPosition;
        GameObject slotObject = Instantiate(blockSlotPrefab, scrollRect.content);
        BlockSlotUI slotUI = slotObject.GetComponent<BlockSlotUI>();
        slotUI.Setup(block, playerInput);
        entries.Add((block, slotUI));
    }

    public void RemoveBlock(Block block)
    {
        int idx = entries.FindIndex(e => e.block == block);
        if (idx < 0) return;
        Destroy(entries[idx].slot.gameObject);
        entries.RemoveAt(idx);
    }
}
