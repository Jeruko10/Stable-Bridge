using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class BlockSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("Settings")]
    [SerializeField] float scale;

    [Header("References")]
    [SerializeField] Image image;
    
    Block block;
    PlayerInput playerInput;

    public void Setup(Block b, PlayerInput input)
    {
        block = b;
        playerInput = input;

        image.sprite = block.Prefab.InterfaceImage;
        image.SetNativeSize();

        RectTransform imgRectTransform = image.GetComponent<RectTransform>();
        imgRectTransform.sizeDelta *= scale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        playerInput.BeginDragFromInventory(block);
    }

    // Required by Unity for OnBeginDrag to fire
    public void OnDrag(PointerEventData eventData) { }
}
