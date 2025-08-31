using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ConditionType
{
    ColorMatch,
    DestroyableTiles
}

public class VictoryConditionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private Image conditionIcon;

    public ConditionType ConditionType { get; private set; }
    public TileType TileType { get; private set; }

    public void Init(string text, Sprite icon, TileType tileType, ColorPalette colorPalette, ConditionType conditionType)
    {
        conditionIcon.sprite = icon;
        conditionText.text = text;
        ConditionType = conditionType;
        TileType = tileType;

        if (conditionType == ConditionType.DestroyableTiles)
        {
            conditionIcon.color = Color.white;
            return;
        }
        int colorIndex = (int)tileType;
        conditionIcon.color = colorPalette.TileColors[colorIndex].Color;
    }

    public void UpdateUI(string conditionText)
    {
        this.conditionText.text = conditionText;
    }
}
