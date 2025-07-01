using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileInputHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private TileView tileView;

    private Vector2 startPointerPos;
    private bool isDragging;

    public void OnPointerDown(PointerEventData eventData)
    {
        startPointerPos = eventData.pressPosition;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) 
            return;

        Vector2 dragDelta = eventData.position - startPointerPos;

        if (dragDelta.magnitude < 50f) 
            return;

        Vector2Int dir = Vector2Int.zero;
        if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y))
            dir = dragDelta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            dir = dragDelta.y > 0 ? Vector2Int.up : Vector2Int.down;

        GridController.Instance.TrySwapTiles(tileView.Data.GridPosition, dir);

        isDragging = false; // prevent multiple swaps
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }
}

