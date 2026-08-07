public enum DebugActionKind
{
    Button,
    Toggle,
    IntField
}

public interface IDebugAction
{
    string Id { get; }
    string Category { get; }
    string Label { get; }
    DebugActionKind Kind { get; }

    bool IsAvailable(DebugContext context);
    void Execute(DebugContext context, int intValue = 0);
    bool GetToggleState(DebugContext context);
}
