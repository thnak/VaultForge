using ApexCharts;
using Color = ApexCharts.Color;

namespace WebApp.Client.Utils;

public static class ApexChartExtension
{
    public static ApexChartOptions<T> InitChartOptions<T>() where T : class
    {
        return new ApexChartOptions<T>()
        {
            Theme = new Theme(),
            Chart = new Chart()
            {
                FontFamily = "Roboto', 'Helvetica', 'Arial', 'sans-serif",
                Background = "transparent",
            },
            Tooltip = new() { Intersect = false },
            PlotOptions = new PlotOptions(),
            Legend = new Legend()
            {
                Labels = new LegendLabels()
                {
                    Colors = new Color(["#000000"])
                }
            }
        };
    }

    public static async Task UpdateOptionsSafetyAsync<T>(this ApexChart<T>? chart) where T : class
    {
        if (chart is not null)
        {
            await chart.UpdateOptionsAsync(false, true, false);
        }
    }

    public static void DisposeSafety<T>(this ApexChart<T>? chart) where T : class
    {
        if (chart is not null)
        {
            chart.Dispose();
        }
    }
}