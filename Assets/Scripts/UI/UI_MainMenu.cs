using TMPro;
using UnityEngine;

public class UI_MainMenu : UIMenu
{
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI levelText;

    private ISaveService saveService;
    private int levelIndex;

    private void Start()
    {
        levelPanel.SetActive(true);
        settingsPanel.SetActive(false);

        saveService = Services.Get<ISaveService>();
        levelIndex = saveService.PlayerData.nextLevelIndex;

        LoadInitialValues();
    }

    private void LoadInitialValues()
    {
        coinCountText.text = "x " + saveService.PlayerData.coins.ToString();
        levelText.text = "Level " + (levelIndex + 1).ToString();
    }

    public void PlayButtonPressed()
    {
        Debug.Log("Play Button pressed, loading level " + (levelIndex + 1));
        StartCoroutine(Loader.CallDelayedLoad(Loader.Scene.PlayScene));
    }

    public void SettingsButtonPressed()
    {
        Debug.Log("Settings Button pressed, not yet implemented");
    }
}
