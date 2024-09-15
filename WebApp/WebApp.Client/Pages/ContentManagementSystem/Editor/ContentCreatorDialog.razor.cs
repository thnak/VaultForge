using BusinessModels.Advertisement;
using BusinessModels.Converter;
using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace WebApp.Client.Pages.ContentManagementSystem.Editor;

public partial class ContentCreatorDialog : ComponentBase, IDisposable, IAsyncDisposable
{
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;
    [Parameter] public ArticleModel? Article { get; set; }


    private MudForm? _form;

    private ArticleModel _article = new();

    private static readonly Dictionary<string, string> Flags = new()
    {
        {
            "vi-VN", "/images/Flag_of_Vietnam.png"
        },
        {
            "en-US", "/images/Flag_of_the_United_States.png"
        },
        {
            "de-DE", "/images/Flag_of_Germany.svg"
        },
        {
            "ja-JP", "/images/Flag_of_Japan.svg"
        },
        {
            "ko-KR", "/images/Flag_of_Korea.png"
        },
        {
            "es-ES", "/images/Flag_of_Spain.png"
        },
        {
            "zh-CN", "/images/Flag_of_China.png"
        }
    };

    private OrderModelFluentValidator? _fluentValidator;
    private bool Processing { get; set; }
    private HubConnection? Hub { get; set; }
    private readonly CancellationTokenSource _cts = new();
    private string CurrentTag { get; set; } = string.Empty;
    private Random Random { get; } = new();

    #region Validate model

    private class OrderModelFluentValidator : AbstractValidator<ArticleModel>
    {
        HubConnection Hub { get; set; }

        public OrderModelFluentValidator(HubConnection hubConnection)
        {
            Hub = hubConnection;
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage(AppLang.ThisFieldIsRequired)
                .Length(1, 100).MustAsync(TitleCheckAsync).WithMessage(Existing);

            RuleFor(x => x.Language)
                .NotEmpty().WithMessage(AppLang.ThisFieldIsRequired)
                .Length(1, 100).MustAsync(TitleCheckAsync).WithMessage(Existing);

            RuleFor(x => x.Summary)
                .NotEmpty().WithMessage(AppLang.ThisFieldIsRequired)
                .Length(1, 100);
        }

        private string Existing(ArticleModel arg)
        {
            return AppLang.Content_already_exists + $". {arg.Title} ({arg.Language})";
        }

        private Task<bool> TitleCheckAsync(ArticleModel arg1, string arg2, CancellationToken arg3)
        {
            return IsUniqueAsync(arg1.Title, arg1.Language, arg3);
        }

        private async Task<bool> IsUniqueAsync(string title, string language, CancellationToken cancellationToken = default)
        {
            var result = await Hub.InvokeAsync<bool>("CheckExistByTitleAndLanguage", title, language, cancellationToken);
            return result != true;
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<ArticleModel>.CreateWithOptions((ArticleModel)model, x => x.IncludeProperties(propertyName)));
            if (result.IsValid)
                return Array.Empty<string>();
            return result.Errors.Select(e => e.ErrorMessage);
        };
    }

    #endregion

    protected override async Task OnParametersSetAsync()
    {
        _article = Article != null ? Article.Copy() : _article;
        Hub = Navigation.ToAbsoluteUri("/PageCreatorHub").InitHub();

        await Hub.StartAsync();
        _fluentValidator = new OrderModelFluentValidator(Hub);
        await base.OnParametersSetAsync();
    }

    private void Cancel()
    {
        DialogInstance.Cancel();
    }

    private async Task DialogSubmit()
    {
        Processing = true;
        if (_form != null)
        {
            await _form.Validate();
            if (_form.IsValid)
            {
                var result = await Hub!.InvokeAsync<SignalRResult>("CreateAdvertisement", _article, _cts.Token);
                if (result.Success)
                {
                    ToastService.ShowSuccess(result.Message);
                    DialogInstance.Close(_article);
                }
                else
                {
                    ToastService.ShowError(result.Message);
                }
            }
        }

        Processing = false;
    }


    public void Dispose()
    {
        _form?.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Hub != null) await Hub.DisposeAsync();
    }

    private Task AddTag()
    {
        _article.Keywords.Add(CurrentTag);
        CurrentTag = string.Empty;
        return InvokeAsync(StateHasChanged);
    }
    
    private Task RemoveKeyWords(MudChip<string> obj)
    {
        if (obj.Value != null)
        {
            _article.Keywords.Remove(obj.Value);
        }

        return InvokeAsync(StateHasChanged);
    }

    private Color? RandomColor()
    {
        Color[] colors = [Color.Dark, Color.Default, Color.Error, Color.Info, Color.Success, Color.Primary, Color.Secondary, Color.Tertiary];
        int index = Random.Next(colors.Length);
        return colors[index];
    }
}