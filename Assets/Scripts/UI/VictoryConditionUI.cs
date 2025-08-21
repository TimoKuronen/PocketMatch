using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryConditionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private Image conditionIcon;

    public void Init(VictoryConditions victoryConditions)
    {

    }

    public void UpdateUI(string conditionText)
    {
        this.conditionText.text = conditionText;
    }
}
