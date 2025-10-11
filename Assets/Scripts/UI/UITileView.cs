using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITileView : MonoBehaviour
{
    [SerializeField] private Image image;

    public RectTransform RectTransform { get; private set; }
    public TileData Data { get; private set; }

    public void Init(TileData data, Sprite sprite)
    {
        Data = data;
        image.sprite = sprite;
    }

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }
}
