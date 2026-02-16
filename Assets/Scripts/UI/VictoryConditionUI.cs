using System.Text;
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

    private readonly StringBuilder sb = new StringBuilder(8);

    public ConditionType ConditionType { get; private set; }
    public TileType TileType { get; private set; }

    public void Init(int count, Sprite icon, TileType tileType, ConditionType conditionType)
    {
        conditionIcon.sprite = icon;
        ConditionType = conditionType;
        TileType = tileType;
        SetCountText(count);

        if (conditionType == ConditionType.DestroyableTiles)
        {
            conditionIcon.color = Color.white;
        }
    }

    public void UpdateUI(int count)
    {
        SetCountText(count);
    }

    private void SetCountText(int count)
    {
        sb.Clear();
        sb.Append(count);
        conditionText.text = sb.ToString();
    }
}
