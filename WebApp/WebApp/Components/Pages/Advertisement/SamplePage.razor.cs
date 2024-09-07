using System.Globalization;
using System.Web;
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
            ContentId = HttpUtility.UrlDecode(ContentId);
            articleModel = AdvertisementService.Get(ContentId);
            if (articleModel == null)
            {
                articleModel = AdvertisementService.Get(ContentId, CultureInfo.CurrentUICulture.Name);
            }

            if (articleModel != null)
            {
                Metadata.Add(new Dictionary<string, string>() { { "name", "title" }, { "content", articleModel.Title } });
                Metadata.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", articleModel.Summary } });
                Metadata.Add(new Dictionary<string, string>() { { "name", "keywords" }, { "content", string.Join(", ", articleModel.Keywords) } });
                Metadata.Add(new Dictionary<string, string>() { { "name", "image" }, { "content", articleModel.Image } });

                Title = articleModel.Title;
                RenderPage(articleModel.StyleSheet, articleModel.HtmlSheet, articleModel.JavaScriptSheet);
            }
        }

        if (articleModel == null)
        {
            HttpContext?.RedirectTo(PageRoutes.Error.NotFound);
            return;
        }


        base.OnInitialized();
    }


    private void RenderPage(params string[] htmlContent)
    {
        int index = 0;
        PageRenderFragment = builder =>
        {
            foreach (var content in htmlContent)
            {
                if (index == 0)
                {
                    builder.OpenElement(index++, "style");
                    builder.AddMarkupContent(index++, content);
                    builder.CloseElement();
                }
                else
                {
                    builder.AddMarkupContent(index++, content);

                }
            }
        };

        InvokeAsync(StateHasChanged);
    }
}