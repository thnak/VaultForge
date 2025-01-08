﻿using System.Text.Json;
using ApexCharts;
using Blazored.Toast;
using BlazorWorker.Core;
using BusinessModels.Converter;
using BusinessModels.System.InternetOfThings;
using MudBlazor;
using MudBlazor.Services;
using WebApp.Client.Services.UserInterfaces;

namespace WebApp.Client.Services;

public static class FrontEnd
{
    public static IServiceCollection AddFrontEndService(this IServiceCollection service)
    {
        service.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 10000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            config.PopoverOptions.CheckForPopoverProvider = false;
            config.ResizeObserverOptions.EnableLogging = true;
            config.ResizeObserverOptions.ReportRate = 500;
        });
        service.AddBlazoredToast();
        // service.AddWorkerFactory();
        service.AddScoped<ProtectedLocalStorage>();
        service.AddScoped<ProtectedSessionStorage>();


        service.AddSingleton(new JsonSerializerOptions
        {
            Converters = { new ObjectIdConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
        service.AddApexCharts(e =>
        {
            e.GlobalOptions = new ApexChartBaseOptions
            {
                Debug = true,
                Theme = new Theme { Palette = PaletteType.Palette6 },
            };
        });
        return service;
    }


    /// <summary>
    /// tương tự như phiên bản singleton nhưng dành cho server side
    /// </summary>
    /// <param name="service"></param>
    /// <returns></returns>
    public static IServiceCollection AddFrontEndScopeService(this IServiceCollection service)
    {
        service.AddScoped<StateContainer>();
        service.AddScoped<DocumentObjectModelEventListener>();
        service.AddScoped<IIndexedDbService<IoTDevice>, IndexedDbService<IoTDevice>>();
        service.AddScoped<IWorkerFactory, WorkerFactory>();

        return service;
    }

    /// <summary>
    /// tương tự như phiên bản scope nhưng dành cho client side
    /// </summary>
    /// <param name="service"></param>
    /// <returns></returns>
    public static IServiceCollection AddFrontEndSingletonService(this IServiceCollection service)
    {
        service.AddSingleton<StateContainer>();
        service.AddSingleton<DocumentObjectModelEventListener>();
        service.AddSingleton<IIndexedDbService<IoTDevice>, IndexedDbService<IoTDevice>>();

        return service;
    }
}