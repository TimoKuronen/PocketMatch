using UnityEngine;
using UnityEngine.EventSystems;

public class TileInputHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Tile View Reference")]
    [SerializeField] private TileView tileView;

    private Vector2 startPointerPos;
    private bool isDragging;

    [Header("Double Tap Settings")]
    [SerializeField] private float doubleTapTimeThreshold = 0.5f;
    private int tapCount = 0;
    private float lastTapTime;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (tileView.Data.State != TileState.Normal)
        {
            return;
        }

#if UNITY_EDITOR
        /// Destroying single tiles for debugging purposes
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            GridController.Instance.DestroyTargetTile(tileView.Data.GridPosition);
            return;
        }
#endif
        startPointerPos = eventData.pressPosition;
        isDragging = true;

        CheckDoubleTap();
    }

    private void CheckDoubleTap()
    {
        float currentTime = Time.time;
        if (currentTime - lastTapTime < doubleTapTimeThreshold)
        {
            tapCount++;
            if (tapCount == 2)
            {
                HandleDoubleTap();
                tapCount = 0;
            }
        }
        else
        {
            tapCount = 1;
        }
        lastTapTime = currentTime;
    }

    private void HandleDoubleTap()
    {
        //Debug.Log("Double Tap Detected");

        isDragging = false;

        GridController.Instance.AttemptPowerTrigger(tileView);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (tileView.Data.State != TileState.Normal)
        {
            return;
        }

        if (!isDragging)
            return;

        Vector2 dragDelta = eventData.position - startPointerPos;

        if (dragDelta.magnitude < 35f)
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

