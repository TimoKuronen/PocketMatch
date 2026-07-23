using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectButtonView : MonoBehaviour{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private GameObject lockedStateRoot;
    [SerializeField] private GameObject currentStateRoot;

    private int levelIndex;
    private Action<int> onSelected;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClicked);
    }

    public void Bind(int zeroBasedLevelIndex, bool isUnlocked, bool isCurrent, Action<int> onSelectedCallback)
    {
        levelIndex = zeroBasedLevelIndex;
        onSelected = onSelectedCallback;

        if (levelLabel != null)
            levelLabel.text = (zeroBasedLevelIndex + 1).ToString();

        if (button != null)
            button.interactable = isUnlocked;

        if (lockedStateRoot != null)
            lockedStateRoot.SetActive(!isUnlocked);

        if (currentStateRoot != null)
            currentStateRoot.SetActive(isCurrent);
    }

    private void HandleClicked()
    {
        onSelected?.Invoke(levelIndex);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }
}
