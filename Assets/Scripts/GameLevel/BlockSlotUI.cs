using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlockSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] TMP_Text label;

    Block block;
    PlayerInput playerInput;

    public void Setup(Block b, PlayerInput input)
    {
        block = b;
        playerInput = input;

        if (label != null) label.text = block.name;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        playerInput.BeginDragFromInventory(block);
    }

    // Required by Unity for OnBeginDrag to fire
    public void OnDrag(PointerEventData eventData) { }
}
