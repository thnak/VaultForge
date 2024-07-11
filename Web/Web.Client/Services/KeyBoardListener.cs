using Microsoft.JSInterop;

namespace Web.Client.Services;

public class KeyBoardListener
{
    #region Enter

    public static Func<Task>? EnterClickedAsync { get; set; }
    public static Action? EnterClicked { get; set; }

    [JSInvokable]
    public static void EnterEvenListener()
    {
        EnterClickedAsync?.Invoke();
        EnterClicked?.Invoke();
    }

    #endregion
}