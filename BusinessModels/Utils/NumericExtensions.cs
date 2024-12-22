namespace BusinessModels.Utils;

public static class NumericExtensions
{
    public static float Round(this float value, int decimalPlaces)
    {
        return MathF.Round(value, decimalPlaces);
    }

    public static double Round(this double value, int decimalPlaces)
    {
        return Math.Round(value, decimalPlaces);
    }
}