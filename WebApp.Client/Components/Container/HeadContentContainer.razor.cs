﻿using BusinessModels.Resources;
using BusinessModels.Validator.Compare;
using Microsoft.AspNetCore.Components;

namespace WebApp.Client.Components.Container;

public partial class HeadContentContainer : ComponentBase, IDisposable
{
    [Parameter] public List<Dictionary<string, string>> MetaProperty { get; set; } = [];
    [Parameter] public List<Dictionary<string, string>> LinkProperty { get; set; } = [];


    private List<Dictionary<string, string>> Properties { get; set; } = [];
    private List<Dictionary<string, string>> PreviousMetaProperties { get; set; } = [];
    private RenderFragment? MetaRenderFragment { get; set; }
    private bool ShouldRen { get; set; } = true;

    protected override bool ShouldRender()
    {
        return ShouldRen;
    }

    protected override async Task OnParametersSetAsync()
    {
        await OnChangedAsync();
        await base.OnParametersSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            CustomStateContainer.OnChangedAsync += OnChangedAsync;
        }

        base.OnAfterRender(firstRender);
    }

    private Task OnChangedAsync()
    {
        Properties =
        [
            ..MetaProperty,
            new Dictionary<string, string>
            {
                { "name", "theme-color" },
                { "content", CustomStateContainer.SharedPalette.Primary.Value }
            },

            new Dictionary<string, string>
            {
                { "name", "background-color" },
                { "content", CustomStateContainer.SharedPalette.AppbarBackground.Value }
            },

            new Dictionary<string, string>
            {
                { "name", "type" },
                { "content", "website" }
            },

            new Dictionary<string, string>
            {
                { "name", "robot" },
                { "content", "noodp, noydir, max-image-preview:large" }
            },
            new Dictionary<string, string>
            {
                { "name", "google" },
                { "content", "notranslate" }
            },
            new Dictionary<string, string>
            {
                { "name", "author" },
                { "content", "https://github.com/thnak" }
            },
            new Dictionary<string, string>
            {
                { "name", "publisher" },
                { "content", "https://github.com/thnak" }
            },
            new Dictionary<string, string>
            {
                { "name", "copyright" },
                { "content", "https://github.com/thnak" }
            },
            new Dictionary<string, string>
            {
                { "name", "url" },
                { "content", Navigation.BaseUri }
            },
            new Dictionary<string, string>
            {
                { "name", "site_name" },
                { "content", "VaultForge" }
            },
        ];

        int index = 0;
        string[] extendMetaNames = ["title", "description", "image", "site_name", "url", "type", "card", "author"];
        string[] extend = ["ogg", "twitter", "facebook", "linkedin"];
        foreach (var meta in Properties.ToArray())
        {
            foreach (var name in extendMetaNames)
            {
                if (meta.ContainsValue(name))
                {
                    foreach (var value in extend)
                    {
                        var newPair = new Dictionary<string, string> { { "name", $"{value}:{Properties[index]["name"]}" }, { "content", Properties[index]["content"] } };
                        Properties.Add(newPair);
                    }
                }
            }

            index += 1;
        }

        ShouldRen = !PreviousMetaProperties.Equal(Properties);
        if (ShouldRen)
        {
            PreviousMetaProperties = Properties;
            MetaRenderFragment = CreateComponent();
            return InvokeAsync(StateHasChanged);
        }
        return Task.CompletedTask;
    }

    private RenderFragment CreateComponent() => builder =>
    {
        var index = 0;

        foreach (var dictionary in Properties)
        {
            builder.OpenElement(index++, "meta");
            int attributeIndex = 0;
            foreach (var pair in dictionary)
            {
                builder.AddAttribute(attributeIndex++, pair.Key, pair.Value);
            }

            builder.CloseElement();
        }

        foreach (var dictionary in LinkProperty)
        {
            builder.OpenElement(index++, "link");
            int attributeIndex = 0;
            foreach (var pair in dictionary)
            {
                builder.AddAttribute(attributeIndex++, pair.Key, pair.Value);
            }

            builder.CloseElement();
        }

        builder.OpenElement(index++, "link");
        builder.AddAttribute(1, "rel", "canonical");
        builder.AddAttribute(2, "href", Navigation.Uri);
        builder.CloseElement();

        builder.OpenElement(index++, "link");
        builder.AddAttribute(1, "rel", "alternate");
        builder.AddAttribute(2, "href", Navigation.Uri);
        builder.AddAttribute(3, "hreflang", "x-default");
        builder.CloseElement();

        foreach (var culture in AllowedCulture.SupportedCultures)
        {
            builder.OpenElement(index++, "link");
            builder.AddAttribute(1, "rel", "alternate");
            builder.AddAttribute(2, "href", Navigation.Uri + $"&lang={culture.Name}");
            builder.AddAttribute(3, "hreflang", culture.Name);
            builder.CloseElement();
        }
        
       
    };

    public void Dispose()
    {
        CustomStateContainer.OnChangedAsync -= OnChangedAsync;
    }
}