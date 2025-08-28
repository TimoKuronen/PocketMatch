using System;
using TMPro;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    [SerializeField] GameObject levelPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] TextMeshProUGUI versionText;
    [SerializeField] TextMeshProUGUI coinCountText;
    [SerializeField] TextMeshProUGUI levelText;

    private ISaveService saveService;
    private int levelInde;

    void Start()
    {
        levelPanel.SetActive(true);
        settingsPanel.SetActive(false);

        saveService = Services.Get<ISaveService>();
        levelInde = saveService.PlayerData.nextLevelIndex;

        LoadInitialValues();
    }

    private void LoadInitialValues()
    {
        coinCountText.text = "x " + saveService.PlayerData.nextLevelIndex.ToString();
        levelText.text = "Level " + (levelInde + 1).ToString();
    }

    void OnPlayButtonPressed()
    {

    }
    void OnSettingsButtonPressed()
    {

    }
}
