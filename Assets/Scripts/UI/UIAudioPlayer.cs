using UnityEngine;

public class UIAudioPlayer : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;

    [SerializeField] private AudioCue openMenuSFX;
    [SerializeField] private AudioCue closeMenuSFX;
    [SerializeField] private AudioCue buttonPressSFX;

    private UIMenu uiMenu;
    private IAudioService soundService;

    void Start()
    {
        uiMenu = GetComponent<UIMenu>();

        uiMenu.OnMenuOpened += OnMenuOpened;
        uiMenu.OnMenuClosed += OnMenuClosed;
        uiMenu.OnButtonPressed += OnButtonClicked;
    }
    private void OnMenuOpened() => soundService.Play(openMenuSFX, audioSource);
    private void OnMenuClosed() => soundService.Play(closeMenuSFX, audioSource);
    private void OnButtonClicked() => soundService.Play(buttonPressSFX, audioSource);

    private void OnDestroy()
    {
        uiMenu.OnMenuOpened -= OnMenuOpened;
        uiMenu.OnMenuClosed -= OnMenuClosed;
        uiMenu.OnButtonPressed -= OnButtonClicked;
    }
}
