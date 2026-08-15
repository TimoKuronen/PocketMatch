using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectPanel : UIMenu, ILevelSelectView
{    [SerializeField] private Button backButton;
    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private LevelSelectButtonView levelButtonPrefab;

    private readonly List<LevelSelectButtonView> spawnedButtons = new List<LevelSelectButtonView>();

    public event Action<int> LevelSelected;
    public event Action BackClicked;
    public event Action DisplayRequested;

    protected override void Awake()
    {
        base.Awake();
        menuType = MenuType.LevelSelectMenu;

        if (menuPanel != null && menuPanel == transform.root.gameObject)
            menuPanel = gameObject;

        if (backButton != null)
            backButton.onClick.AddListener(() => BackClicked?.Invoke());

        ValidateReferences();
    }

    public override void Open()
    {
        base.Open();
        DisplayRequested?.Invoke();
    }

    public void BindLevels(int totalLevels, int unlockedThroughIndex, int highlightedIndex)
    {
        if (!HasValidReferences())
        {
            Debug.LogError("[LevelSelectPanel] Cannot bind levels: required references are missing.");
            return;
        }

        ClearSpawnedButtons();

        for (int i = 0; i < totalLevels; i++)
        {
            bool isUnlocked = i <= unlockedThroughIndex;
            bool isCurrent = i == highlightedIndex;

            var buttonView = Instantiate(levelButtonPrefab, levelButtonContainer);
            buttonView.gameObject.SetActive(true);
            buttonView.Bind(i, isUnlocked, isCurrent, HandleLevelSelected);
            spawnedButtons.Add(buttonView);
        }
    }

    private void HandleLevelSelected(int levelIndex)
    {
        LevelSelected?.Invoke(levelIndex);
    }

    private void ClearSpawnedButtons()
    {
        foreach (var buttonView in spawnedButtons)
        {
            if (buttonView != null)
                Destroy(buttonView.gameObject);
        }

        spawnedButtons.Clear();
    }

    private bool HasValidReferences()
    {
        return backButton != null && levelButtonContainer != null && levelButtonPrefab != null;
    }

    private void ValidateReferences()
    {
        if (HasValidReferences())
            return;

        Debug.LogError("[LevelSelectPanel] Required references are not configured.");
    }

    protected override void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();

        ClearSpawnedButtons();
        base.OnDestroy();
    }
}
