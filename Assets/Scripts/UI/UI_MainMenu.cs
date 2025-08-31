using TMPro;
using UnityEngine;

public class UI_MainMenu : UIMenu
{
    [SerializeField] GameObject levelPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] TextMeshProUGUI versionText;
    [SerializeField] TextMeshProUGUI coinCountText;
    [SerializeField] TextMeshProUGUI levelText;

    private ISaveService saveService;
    private int levelInde;

    private void Start()
    {
        levelPanel.SetActive(true);
        settingsPanel.SetActive(false);

        saveService = Services.Get<ISaveService>();
        levelInde = saveService.PlayerData.nextLevelIndex;

        LoadInitialValues();
    }

    private void LoadInitialValues()
    {
        coinCountText.text = "x " + saveService.PlayerData.coins.ToString();
        levelText.text = "Level " + (levelInde + 1).ToString();
    }

    public void PlayButtonPressed()
    {
        Debug.Log("Play Button pressed, loading level " + levelInde);
        StartCoroutine(Loader.CallDelayedLoad(Loader.Scene.PlayScene));
    }

    public void SettingsButtonPressed()
    {
        Debug.Log("Settings Button pressed, not yet implemented");
    }
}
