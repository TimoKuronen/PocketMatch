using Cysharp.Threading.Tasks;
using VContainer.Unity;

/// <summary>Asks once for analytics consent when the main menu loads without a stored answer.</summary>
public sealed class AnalyticsConsentPresenter : IStartable
{
    private readonly IAnalyticsConsent consent;
    private readonly ConfirmationDialog dialog;
    private readonly MenuStackManager menuStackManager;
    private readonly ILocalizationService localization;

    public AnalyticsConsentPresenter(
        IAnalyticsConsent consent,
        ConfirmationDialog dialog,
        MenuStackManager menuStackManager,
        ILocalizationService localization)
    {
        this.consent = consent;
        this.dialog = dialog;
        this.menuStackManager = menuStackManager;
        this.localization = localization;
    }

    public void Start()
    {
        if (consent.State == AnalyticsConsentState.Unknown)
            PromptWhenReadyAsync().Forget();
    }

    private async UniTaskVoid PromptWhenReadyAsync()
    {
        var token = dialog.GetCancellationTokenOnDestroy();
        await UniTask.WaitUntil(() => localization.IsReady, cancellationToken: token);

        dialog.Setup(
            localization.Get(LocalizationKeys.PrivacyConsentMessage),
            () => consent.SetGranted(true),
            () => consent.SetGranted(false));
        menuStackManager.PushMenu(dialog);
    }
}
