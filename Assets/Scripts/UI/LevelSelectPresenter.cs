public class LevelSelectPresenter
{
    private readonly ILevelSelectView view;
    private readonly ISaveService saveService;
    private readonly MenuStackManager menuStackManager;

    public LevelSelectPresenter(
        ILevelSelectView view,
        ISaveService saveService,
        MenuStackManager menuStackManager)
    {
        this.view = view;
        this.saveService = saveService;
        this.menuStackManager = menuStackManager;

        // Wiring for events and data population will be added when the actual
        // level select UI is implemented.
    }
}

