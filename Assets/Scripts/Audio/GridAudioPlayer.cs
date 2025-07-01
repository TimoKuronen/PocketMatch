using UnityEngine;

public class GridAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioCue tileHitAudio;
    [SerializeField] private AudioCue tileMatchAudio;
    [SerializeField] private AudioCue tileSwitchErrorAudio;
    [SerializeField] private AudioCue tileDestroyAudio;

    private ISoundManager soundManager;

    private void Start()
    {
        soundManager = Services.Get<ISoundManager>();

        GridController.Instance.TileDrop += PlayHitAudio;
        GridController.Instance.TileSwapped += PlayMatchAudio;
        GridController.Instance.TileSwapError += PlaySwitchErrorAudio;
        GridController.Instance.TileDestroyed += PlayDestroyAudio;
    }

    private void PlayDestroyAudio()
    {
        soundManager.Play(tileDestroyAudio, GridController.Instance.AudioSource);
    }

    private void PlaySwitchErrorAudio()
    {
        soundManager.Play(tileSwitchErrorAudio, GridController.Instance.AudioSource);
    }

    private void PlayMatchAudio()
    {
        soundManager.Play(tileMatchAudio, GridController.Instance.AudioSource);
    }

    private void PlayHitAudio()
    {
        soundManager.Play(tileHitAudio, GridController.Instance.AudioSource);
    }

    private void OnDestroy()
    {
        GridController.Instance.TileDrop -= PlayHitAudio;
        GridController.Instance.TileSwapped -= PlayMatchAudio;
        GridController.Instance.TileSwapError -= PlaySwitchErrorAudio;
        GridController.Instance.TileDestroyed -= PlayDestroyAudio;
    }
}
