using UnityEngine;
using VContainer;

public class UIAudioPlayer : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;

    [SerializeField] private AudioCue openMenuSFX;
    [SerializeField] private AudioCue closeMenuSFX;
    [SerializeField] private AudioCue buttonPressSFX;

    private UIMenu uiMenu;
    private IAudioService audioService;

    [Inject]
    public void Construct(IAudioService audioService)
    {
        this.audioService = audioService;
        Debug.Log(gameObject + "UIAudioPlayer Constructed with IAudioService dependency.");
    }

    void Start()
    {
        uiMenu = GetComponent<UIMenu>();

        uiMenu.OnMenuOpened += OnMenuOpened;
        uiMenu.OnMenuClosed += OnMenuClosed;
        uiMenu.OnButtonPressed += OnButtonClicked;
    }
    private void OnMenuOpened() => audioService.Play(openMenuSFX, audioSource);
    private void OnMenuClosed() => audioService.Play(closeMenuSFX, audioSource);
    private void OnButtonClicked() => audioService.Play(buttonPressSFX, audioSource);

    private void OnDestroy()
    {
        uiMenu.OnMenuOpened -= OnMenuOpened;
        uiMenu.OnMenuClosed -= OnMenuClosed;
        uiMenu.OnButtonPressed -= OnButtonClicked;
    }
}
