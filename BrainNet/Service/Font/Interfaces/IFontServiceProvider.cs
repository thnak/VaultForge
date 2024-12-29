using SixLabors.Fonts;

namespace BrainNet.Service.Font.Interfaces;

public interface IFontServiceProvider
{
    SixLabors.Fonts.Font CreateFont(Model.FontFamily family,float size, FontStyle style);
    System.Drawing.Font CreateFont(Model.FontFamily family, float size, System.Drawing.FontStyle style);
}