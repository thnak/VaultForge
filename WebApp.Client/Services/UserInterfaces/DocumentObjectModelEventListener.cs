using Microsoft.JSInterop;

namespace WebApp.Client.Services.UserInterfaces;

public class DocumentObjectModelEventListener : IDisposable
{
    private readonly Dictionary<string, Func<Task>> _registeredEvents = [];
    private readonly IJSRuntime _jsRuntime;
    private readonly DotNetObjectReference<DocumentObjectModelEventListener> _dotNetRef;
    private readonly ILogger<DocumentObjectModelEventListener> _logger;

    public DocumentObjectModelEventListener(IJSRuntime jsRuntime, ILogger<DocumentObjectModelEventListener> logger)
    {
        _jsRuntime = jsRuntime;
        _dotNetRef = DotNetObjectReference.Create(this);
        _logger = logger;
    }


    public ValueTask AddEventListenerAsync(string elementId, DomEventName eventName, Func<Task> callback, bool preventDefault = false)
    {
        string eventNameStr = eventName.ToString().ToLower(); // Convert enum to lowercase for JavaScript compatibility
        string key = GenerateKey(elementId, eventNameStr);

        // Add to the static dictionary
        if (_registeredEvents.TryAdd(key, callback))
        {
            try
            {
                return _jsRuntime.InvokeVoidAsync("eventListenerInterop.addEventListener", elementId, eventNameStr, _dotNetRef, nameof(OnEventTriggeredEventListener), preventDefault);
            }
            catch (JSDisconnectedException)
            {
                //
            }
        }
        else
        {
            _logger.LogWarning($"Event {eventNameStr} for element {elementId} is already registered.");
            return ValueTask.CompletedTask;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveEventListenerAsync(string elementId, DomEventName eventName)
    {
        string eventNameStr = eventName.ToString().ToLower(); // Convert enum to lowercase for JavaScript compatibility
        string key = GenerateKey(elementId, eventNameStr);

        try
        {
            if (_registeredEvents.Remove(key))
            {
                return _jsRuntime.InvokeVoidAsync("eventListenerInterop.removeEventListener", elementId, eventNameStr);
            }
        }
        catch (JSDisconnectedException e)
        {
            return ValueTask.CompletedTask;
        }

        return ValueTask.CompletedTask;
    }

    [JSInvokable("OnEventTriggeredEventListener")]
    public void OnEventTriggeredEventListener(string key)
    {
        if (_registeredEvents.TryGetValue(key, out var callback))
        {
            callback.Invoke();
        }
    }

    private static string GenerateKey(string elementId, string eventName)
    {
        return $"{elementId}:{eventName}";
    }

    public void Dispose()
    {
        ContextMenuClickedAsync = null;
        ContextMenuClicked = null;

        Offline = null;
        OfflineAsync = null;

        Online = null;
        OnlineAsync = null;

        PageHideEvent = null;
        PageHideEventAsync = null;

        PageShowEvent = null;
        PageShowEventAsync = null;

        InstalledEvent = null;
        InstalledEventAsync = null;

        VisibilityChangeEvent = null;
        VisibilityChangeEventAsync = null;

        FullScreenChangeEventAsync = null;
        FullScreenChangeEvent = null;

        KeyPressChangeEventAsync = null;

        ScrollEventAsync = null;
        ScrollEvent = null;

        ScrollToReloadEventAsync = null;
    }

    #region Context Menu

    public Func<Task>? ContextMenuClickedAsync
    {
        get => SelfContextMenuClickedAsync;
        set => SelfContextMenuClickedAsync = value;
    }

    public Func<int, int, Task>? ContextMenuClickedWithParamAsync
    {
        get => SelfContextMenuClickedWithParamAsync;
        set => SelfContextMenuClickedWithParamAsync = value;
    }

    public static Action? ContextMenuClicked
    {
        get => SelfContextMenuClicked;
        set => SelfContextMenuClicked = value;
    }

    private static Func<Task>? SelfContextMenuClickedAsync { get; set; }
    private static Func<int, int, Task>? SelfContextMenuClickedWithParamAsync { get; set; }
    private static Action? SelfContextMenuClicked { get; set; }

    [JSInvokable]
    public static void ContextMenuEventListener()
    {
        SelfContextMenuClickedAsync?.Invoke();
        SelfContextMenuClicked?.Invoke();
    }

    [JSInvokable]
    public static void ContextMenuEventListenerWithParam(int x, int y)
    {
        SelfContextMenuClickedWithParamAsync?.Invoke(x, y);
    }

    #endregion

    #region Offline

    public Func<Task>? OfflineAsync
    {
        get => SelfOfflineClickedAsync;
        set => SelfOfflineClickedAsync = value;
    }

    public Action? Offline
    {
        get => SelfOfflineClicked;
        set => SelfOfflineClicked = value;
    }

    private static Func<Task>? SelfOfflineClickedAsync { get; set; }
    private static Action? SelfOfflineClicked { get; set; }

    [JSInvokable]
    public static void OfflineEventListener()
    {
        SelfOfflineClickedAsync?.Invoke();
        SelfOfflineClicked?.Invoke();
    }

    #endregion

    #region Online

    public Func<Task>? OnlineAsync
    {
        get => SelfOnlineAsync;
        set => SelfOnlineAsync = value;
    }

    public Action? Online
    {
        get => SelfOnline;
        set => SelfOnline = value;
    }

    private static Func<Task>? SelfOnlineAsync { get; set; }
    private static Action? SelfOnline { get; set; }

    [JSInvokable]
    public static void OnlineEventListener()
    {
        SelfOnlineAsync?.Invoke();
        SelfOnline?.Invoke();
    }

    #endregion

    #region Page Hide

    public Func<Task>? PageHideEventAsync
    {
        get => SelfPageHideEventAsync;
        set => SelfPageHideEventAsync = value;
    }

    public Action? PageHideEvent
    {
        get => SelfPageHideEvent;
        set => SelfPageHideEvent = value;
    }

    private static Func<Task>? SelfPageHideEventAsync { get; set; }
    private static Action? SelfPageHideEvent { get; set; }

    [JSInvokable]
    public static void PageHideEventEventListener()
    {
        SelfPageHideEventAsync?.Invoke();
        SelfPageHideEvent?.Invoke();
    }

    #endregion

    #region Page Show

    public Func<Task>? PageShowEventAsync
    {
        get => SelfPageShowEventAsync;
        set => SelfPageShowEventAsync = value;
    }

    public Action? PageShowEvent
    {
        get => SelfPageShowEvent;
        set => SelfPageShowEvent = value;
    }

    private static Func<Task>? SelfPageShowEventAsync { get; set; }
    private static Action? SelfPageShowEvent { get; set; }


    [JSInvokable]
    public static void PageShowEventEventListener()
    {
        SelfPageShowEventAsync?.Invoke();
        SelfPageShowEvent?.Invoke();
    }

    #endregion

    #region Installed

    public Func<Task>? InstalledEventAsync
    {
        get => SelfInstalledEventAsync;
        set => SelfInstalledEventAsync = value;
    }

    public Action? InstalledEvent
    {
        get => SelfInstalledEvent;
        set => SelfInstalledEvent = value;
    }

    private static Func<Task>? SelfInstalledEventAsync { get; set; }
    private static Action? SelfInstalledEvent { get; set; }

    [JSInvokable]
    public static void InstalledEventListener()
    {
        SelfInstalledEventAsync?.Invoke();
        SelfInstalledEvent?.Invoke();
    }

    #endregion

    #region visibilitychange

    public Func<bool, Task>? VisibilityChangeEventAsync
    {
        get => SelfVisibilityChangeEventAsync;
        set => SelfVisibilityChangeEventAsync = value;
    }

    public Action? VisibilityChangeEvent
    {
        get => SelfVisibilityChangeEvent;
        set => SelfVisibilityChangeEvent = value;
    }

    private static Func<bool, Task>? SelfVisibilityChangeEventAsync { get; set; }
    private static Action? SelfVisibilityChangeEvent { get; set; }

    [JSInvokable]
    public static void VisibilityChangeEventListener(bool visible)
    {
        SelfVisibilityChangeEventAsync?.Invoke(visible);
        SelfVisibilityChangeEvent?.Invoke();
    }

    #endregion

    #region Full creen changed

    public Func<bool, Task>? FullScreenChangeEventAsync
    {
        get => SelfFullScreenChangeEventAsync;
        set => SelfFullScreenChangeEventAsync = value;
    }

    public Action<bool>? FullScreenChangeEvent
    {
        get => SelfFullScreenChangeEvent;
        set => SelfFullScreenChangeEvent = value;
    }

    private static Func<bool, Task>? SelfFullScreenChangeEventAsync { get; set; }
    private static Action<bool>? SelfFullScreenChangeEvent { get; set; }


    [JSInvokable]
    public static void FullScreenChangeEventListener(bool value)
    {
        SelfFullScreenChangeEventAsync?.Invoke(value);
        SelfFullScreenChangeEvent?.Invoke(value);
    }

    #endregion

    #region Key Press

    public Func<string, Task>? KeyPressChangeEventAsync
    {
        get => SelfKeyPressChangeEventAsync;
        set => SelfKeyPressChangeEventAsync = value;
    }

    private static Func<string, Task>? SelfKeyPressChangeEventAsync { get; set; }


    [JSInvokable]
    public static void KeyPressChangeEventListener(string key)
    {
        SelfKeyPressChangeEventAsync?.Invoke(key);
    }

    #endregion

    #region Scroll

    public static Func<Task>? ScrollEventAsync { get; set; }
    public static Action? ScrollEvent { get; set; }

    [JSInvokable]
    public static void ScrollEventEventListener()
    {
        ScrollEventAsync?.Invoke();
        ScrollEvent?.Invoke();
    }

    #endregion

    #region Scroll to reload

    public Func<Task<bool>>? ScrollToReloadEventAsync
    {
        get => SelfScrollToReloadEventAsync;
        set => SelfScrollToReloadEventAsync = value;
    }

    private static Func<Task<bool>>? SelfScrollToReloadEventAsync { get; set; }

    [JSInvokable]
    public static async Task<bool> ScrollToReloadEventListener()
    {
        if (SelfScrollToReloadEventAsync != null)
        {
            var result = await SelfScrollToReloadEventAsync.Invoke();
            return result;
        }

        return false;
    }

    #endregion


    #region Touch

    public bool IsTouchEnabled => SelfTouchEnabled;

    private static bool SelfTouchEnabled { get; set; }

    [JSInvokable]
    public static Task TouchEventListenerAsync(bool touch)
    {
        SelfTouchEnabled = touch;
        return Task.CompletedTask;
    }

    #endregion
}

public enum DomEventName
{
    // Mouse events
    Click,
    DblClick,
    MouseOver,
    MouseOut,
    MouseEnter,
    MouseLeave,

    // Keyboard events
    KeyDown,
    KeyUp,

    // Form events
    Change,
    Input,
    Submit,

    // Focus events
    Focus,
    Blur,

    // Window events
    Resize,
    ContextMenu
}
