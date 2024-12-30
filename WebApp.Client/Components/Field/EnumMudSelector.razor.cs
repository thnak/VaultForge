using System.Linq.Expressions;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Components.Field;

public partial class EnumMudSelector<T> : ComponentBase where T : struct, Enum
{
    [Parameter] public T SelectedValue { get; set; }
    [Parameter] public EventCallback<T> SelectedValueChanged { get; set; }

    [Parameter] public IEnumerable<T> SelectedValues { get; set; } = [];
    [Parameter] public EventCallback<IEnumerable<T>> SelectedValuesChanged { get; set; }
    [Parameter] public bool MultiSelection { get; set; }
    [Parameter] public bool SelectAll { get; set; }
    [Parameter] public string SelectAllText { get; set; } = AppLang.Select_All;
    [Parameter] public Variant ElementVariant { get; set; }
    [Parameter] public string? LabelString { get; set; }
    [Parameter] public Margin ElementMargin { get; set; }

    [Parameter]
    [Category(CategoryTypes.FormComponent.Validation)]
    public Expression<Func<T>>? For { get; set; }

    private T _selectedValue;
    private List<T> _selectedValues = [];

    protected override void OnParametersSet()
    {
        _selectedValue = SelectedValue;
        _selectedValues = [..SelectedValues];
        base.OnParametersSet();
    }

    private Task _selectedValueChanged(T value)
    {
        _selectedValue = value;
        return SelectedValueChanged.InvokeAsync(value);
    }

    private Task CreateInferredCallback(IEnumerable<T>? arg)
    {
        _selectedValues = [..arg ?? []];
        return SelectedValuesChanged.InvokeAsync(arg ?? []);
    }
}