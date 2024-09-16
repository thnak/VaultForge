using BusinessModels.Advertisement;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebApp.Client.Utils;

namespace WebApp.Client.Components.CodeEditor;

public partial class MonacoCodeEditor : ComponentBase, IDisposable
{
    [Parameter] public required string Id { get; set; }

    [Parameter] public ArticleModel Code { get; set; } = new();
    [Parameter] public EventCallback<ArticleModel> CodeChanged { get; set; }

    private IJSObjectReference? _module;

    #region Static Fields

    private static Func<string, Task>? HtmlCodeOnnChanged { get; set; }
    private static Func<string, Task>? CssCodeOnnChanged { get; set; }
    private static Func<string, Task>? JsCodeOnnChanged { get; set; }

    private static Func<Task<string>>? CssResquest { get; set; }

    #endregion

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.AddScriptResource("https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.51.0/min/vs/loader.js");
            _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/CodeEditor/MonacoCodeEditor.razor.js");
            await _module.InvokeVoidAsync("MonacoCodeEditor.initEditor", "MonacoCodeEditor", Code.HtmlSheet, Code.StyleSheet, Code.JavaScriptSheet);

            HtmlCodeOnnChanged += HtmlCodeOnnChangedHandler;
            CssCodeOnnChanged += CssCodeOnnChangedHandler;
            JsCodeOnnChanged += JsCodeOnnChangedHandler;
            CssResquest += CssResquestHandler;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private Task<string> CssResquestHandler()
    {
        return Task.FromResult(Code.StyleSheet);
    }


    #region Js method

    [JSInvokable]
    public static void HtmlChangeListener(string code)
    {
        HtmlCodeOnnChanged?.Invoke(code);
    }

    [JSInvokable]
    public static void CssChangeListener(string code)
    {
        CssCodeOnnChanged?.Invoke(code);
    }

    [JSInvokable]
    public static void JavascriptChangeListener(string code)
    {
        JsCodeOnnChanged?.Invoke(code);
    }

    [JSInvokable]
    public static Task<string> GetCurrentStyle()
    {
        if (CssResquest != null) return CssResquest.Invoke();
        return Task.FromResult(string.Empty);
    }

    private Task JsCodeOnnChangedHandler(string arg)
    {
        Code.JavaScriptSheet = arg;
        return InvokeAsync(async () => await CodeChanged.InvokeAsync(Code));
    }

    private Task CssCodeOnnChangedHandler(string arg)
    {
        Code.StyleSheet = arg;
        return InvokeAsync(async () => await CodeChanged.InvokeAsync(Code));
    }

    private Task HtmlCodeOnnChangedHandler(string arg)
    {
        Code.HtmlSheet = arg;
        return InvokeAsync(async () => await CodeChanged.InvokeAsync(Code));
    }

    #endregion

    public void Dispose()
    {
        if (_module != null) _module.DisposeAsync().ConfigureAwait(false);
    }
}