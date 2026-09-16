using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingProgressBar : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI textElement;

    private float timer;
    private int dotPhase;
    private string baseMessage = "Loading";
    private ILocalizationService localization;

    private void OnEnable()
    {
        TryBind();
    }

    private void Start()
    {
        if (localization == null)
            TryBind();
    }

    private void OnDisable()
    {
        if (localization == null)
            return;

        localization.LocaleChanged -= OnLocaleChanged;
        localization = null;
    }

    private void TryBind()
    {
        if (localization != null)
            return;

        if (!LocalizationServiceAccess.TryGet(out localization))
            return;

        localization.LocaleChanged += OnLocaleChanged;
        RefreshBaseMessage();
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

    private void OnLocaleChanged()
    {
        RefreshBaseMessage();
    }

    private void RefreshBaseMessage()
    {
        if (localization == null || !localization.IsReady)
            return;

        baseMessage = localization.Get(LocalizationKeys.LoadingMessage);
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
