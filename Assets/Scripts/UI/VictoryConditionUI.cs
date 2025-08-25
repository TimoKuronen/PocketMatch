using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryConditionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private SpriteRenderer conditionIcon;

    public void Init(string text, Sprite icon, ColorPalette colorPalette)
    {
        conditionIcon.sprite = icon;
        conditionText.text = text;
        //conditionIcon.color = 
        //conditionIcon.sprite.c spriteRenderer.color = colorPalette.TileColors[colorIndex].Color;
    }

    public void UpdateUI(string conditionText)
    {
        this.conditionText.text = conditionText;
    }
}
