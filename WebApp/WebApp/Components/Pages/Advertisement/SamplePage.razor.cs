using System.Globalization;
using BusinessModels.Advertisement;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components;
using WebApp.Utils;

namespace WebApp.Components.Pages.Advertisement;

public partial class SamplePage : ComponentBase
{
    #region Parameters

    [CascadingParameter] private HttpContext? HttpContext { get; set; }

    [SupplyParameterFromQuery(Name = "content_id")]
    public string? ContentId { get; set; }

    #endregion

    #region Fields

    private RenderFragment? PageRenderFragment { get; set; }

    private string Title { get; set; } = string.Empty;
    private List<Dictionary<string, string>> Metadata { get; set; } = [];

    #endregion

    protected override void OnInitialized()
    {
        ArticleModel? articleModel = null;
        if (ContentId != null)
        {
            articleModel = AdvertisementService.Get(ContentId, CultureInfo.CurrentUICulture.Name);
            if (articleModel != null)
            {
                Metadata = articleModel.MetaData;
                Metadata.Add(new Dictionary<string, string>() { { "name", "title" }, { "content", articleModel.Title } });
                Metadata.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", articleModel.Summary } });
                Title = articleModel.Title;
            }
        }

        if (articleModel == null)
        {
            HttpContext?.RedirectTo(PageRoutes.Error.NotFound);
            return;
        }


        base.OnInitialized();
    }

    // protected override void OnAfterRender(bool firstRender)
    // {
    //     if (firstRender)
    //     {
    //         if (string.IsNullOrEmpty(Language))
    //         {
    //             Language = CultureInfo.CurrentCulture.Name;
    //             var uri = Navigation.GetUriWithQueryParameters(Navigation.Uri, new Dictionary<string, object?>() { { "language", Language } });
    //             Navigation.NavigateTo(uri, replace: true);
    //             return;
    //         }
    //     }
    //
    //     base.OnAfterRender(firstRender);
    // }


    private void RenderPage(string htmlContent)
    {
        PageRenderFragment = builder => builder.AddMarkupContent(0, htmlContent);
        InvokeAsync(StateHasChanged);
    }
}