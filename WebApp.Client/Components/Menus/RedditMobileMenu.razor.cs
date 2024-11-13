using Microsoft.AspNetCore.Components;

namespace WebApp.Client.Components.Menus;

public partial class RedditMobileMenu : ComponentBase
{
    #region Parameters

    [Parameter] public string Name { get; set; } = string.Empty;
    [Parameter] public List<RedditMobileMenuData> Items { get; set; } = new();

    #endregion

    #region Models

    public class RedditMobileMenuData
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool Disabled { get; set; }
        public Action OnClick { get; set; } = delegate { };
    }

    #endregion

    #region Fields

    private RenderFragment? _menuRenderFragment;
    private bool OpenChild { get; set; }

    #endregion


    protected override void OnParametersSet()
    {
        _menuRenderFragment = CreateItemMenu();
        base.OnParametersSet();
    }

    private void OpenChildMenu()
    {
        OpenChild = !OpenChild;
        InvokeAsync(StateHasChanged);
    }

    private RenderFragment CreateItemMenu()
    {
        return builder =>
        {
            int index = 0;
            builder.OpenElement(0, "div");
            builder.AddAttribute(0, "class", "menu-container");

            // menu header
            builder.OpenElement(index++, "div");
            builder.AddAttribute(0, "class", OpenChild ? "menu-header active" : "menu-header");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, OpenChildMenu));

            if (!string.IsNullOrEmpty(Name))
            {
                builder.OpenElement(1, "span");
                builder.AddAttribute(0, "class", "menu-text");
                builder.AddContent(1, Name);
                builder.CloseElement();
            }

            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "menu-arrow");
            builder.AddMarkupContent(0, "<i class=\"fa-solid fa-angle-down\"></i>");
            builder.CloseElement();

            builder.CloseElement();
            // menu header

            // menu ul

            builder.OpenElement(index, "ul");
            builder.AddAttribute(0, "class", OpenChild ? "menu-items show" : "menu-items");

            int liIndex = 0;
            foreach (var item in Items)
            {
                builder.OpenElement(liIndex++, "li");
                builder.AddAttribute(0, "onclick", EventCallback.Factory.Create(this, item.OnClick));
                builder.AddAttribute(1, "class", "d-flex flex-row align-center");
                builder.AddMarkupContent(2, $"""<i class="{item.Icon}"></i>""");
                builder.AddContent(3, item.Title);
                builder.CloseElement();
            }

            builder.CloseElement();

            // menu ul


            builder.CloseElement();
        };
    }
}