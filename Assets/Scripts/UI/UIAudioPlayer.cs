using UnityEngine;

public class UIAudioPlayer : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;

    [SerializeField] private AudioCue openMenuSFX;
    [SerializeField] private AudioCue closeMenuSFX;
    [SerializeField] private AudioCue buttonPressSFX;

    private UIMenu uiMenu;
    private ISoundManager soundManager;

    void Start()
    {
        uiMenu = GetComponent<UIMenu>();
        soundManager = Services.Get<ISoundManager>();

        uiMenu.MenuOpened += OnMenuOpened;
        uiMenu.MenuClosed += OnMenuClosed;
        uiMenu.ButtonPressed += OnButtonClicked;
    }
    private void OnMenuOpened() => soundManager.Play(openMenuSFX, audioSource);
    private void OnMenuClosed() => soundManager.Play(closeMenuSFX, audioSource);
    private void OnButtonClicked() => soundManager.Play(buttonPressSFX, audioSource);

    private void OnDestroy()
    {
        uiMenu.MenuOpened -= OnMenuOpened;
        uiMenu.MenuClosed -= OnMenuClosed;
        uiMenu.ButtonPressed -= OnButtonClicked;
    }
}
