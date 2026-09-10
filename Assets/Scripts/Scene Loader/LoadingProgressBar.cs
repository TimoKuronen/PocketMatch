using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LoadingProgressBar : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI textElement;

    private float timer;
    private int dotPhase;
    private string baseMessage = "Loading";
    private int refreshVersion;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshBaseMessage();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        refreshVersion++;
    }

    private void Update()
    {
        float progress = Loader.GetLoadingProgress();
        progressBar.fillAmount = Mathf.Clamp01(progress);

        timer += Time.deltaTime;
        if (timer > 0.5f)
        {
            timer = 0;
            UpdateText();
        }
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale _)
    {
        RefreshBaseMessage();
    }

    private void RefreshBaseMessage()
    {
        if (!LocalizationSettings.HasSettings)
            return;

        refreshVersion++;
        RefreshBaseMessageAsync(refreshVersion).Forget();
    }

    private async UniTaskVoid RefreshBaseMessageAsync(int version)
    {
        var init = LocalizationSettings.InitializationOperation;
        await UniTask.WaitUntil(() => init.IsDone || version != refreshVersion);
        if (version != refreshVersion || this == null)
            return;

        await UniTask.Yield(PlayerLoopTiming.Update);
        if (version != refreshVersion || this == null || !isActiveAndEnabled)
            return;

        baseMessage = LocalizationSettings.StringDatabase.GetLocalizedString(
            LocalizationKeys.UiTable,
            LocalizationKeys.LoadingMessage);
        ApplyText();
    }

    private void UpdateText()
    {
        dotPhase = (dotPhase + 1) % 4;
        ApplyText();
    }

    private void ApplyText()
    {
        if (textElement == null)
            return;

        if (dotPhase <= 0)
        {
            textElement.text = baseMessage;
            return;
        }

        textElement.text = baseMessage + new string('.', dotPhase);
    }
}
