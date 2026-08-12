using UnityEngine;
using UnityEngine.EventSystems;

public class TileInputHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    #region Fields

    [Header("Tile View Reference")]
    [SerializeField] private TileView tileView;

    [Header("Double Tap Settings")]
    [SerializeField] private float doubleTapTimeThreshold = 0.5f;

    private Vector2 startPointerPos;
    private bool isDragging;
    private IGridController gridController;
    private int tapCount = 0;
    private float lastTapTime;

    #endregion

    #region Lifecycle

    void OnEnable()
    {
        isDragging = false;
        tapCount = 0;
    }

    #endregion

    #region Public API

    public void Construct(IGridController gridController)
    {
        this.gridController = gridController;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInputAllowed())
        {
            Debug.Log($"Input not allowed because " +
                $"{isDragging} or " +
                $"{gridController.IsProcessingTiles} or " +
                $"{tileView.Data.State == TileState.Normal}");
            return;
        }

#if UNITY_EDITOR
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            gridController.DestroyTargetTile(tileView.Data.GridPosition);
            return;
        }
#endif
        startPointerPos = eventData.pressPosition;
        isDragging = true;

        CheckDoubleTap();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsInputAllowed())
            return;

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

        gridController.TrySwapTiles(tileView.Data.GridPosition, dir);

        isDragging = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    #endregion

    #region Private Helpers

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
        isDragging = false;
        gridController.AttemptPowerTrigger(tileView);
    }

    private bool IsInputAllowed()
    {
        return gridController != null && !gridController.IsProcessingTiles && tileView.Data.State == TileState.Normal;
    }

    #endregion
}
