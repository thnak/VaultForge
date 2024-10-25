using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebApp.Client.Utils;

namespace WebApp.Client.Components.CodeEditor;

public partial class HtmlMonacoEditor : ComponentBase, IDisposable
{
    [Parameter] public string Html { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> HtmlChanged { get; set; }


    private string Id { get; set; } = Guid.NewGuid().ToString();


    private IJSObjectReference? _module;

    #region Static fields

    private static Func<string, Task>? HtmlCodeOnnChanged { get; set; }

    #endregion


    #region Js Methods

    [JSInvokable]
    public static void HtmlChangeListener(string code)
    {
        HtmlCodeOnnChanged?.Invoke(code);
    }

    #endregion


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.AddScriptResource("js/loader.js");
            _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/CodeEditor/HtmlMonacoEditor.razor.js");
            await _module.InvokeVoidAsync("HtmlMonacoEditor.initEditor", Id, Html);
            HtmlCodeOnnChanged += HtmlCodeOnChanged;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task HtmlCodeOnChanged(string arg)
    {
        Html = arg;
        await HtmlChanged.InvokeAsync(arg);
    }

    public void Dispose()
    {
        if(_module != null)
        {
            _module.DisposeAsync().ConfigureAwait(true);
        }
    }
}