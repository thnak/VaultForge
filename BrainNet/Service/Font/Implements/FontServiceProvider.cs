using BrainNet.Assets;
using BrainNet.Service.Font.Interfaces;
using SixLabors.Fonts;
using FontFamily = BrainNet.Service.Font.Model.FontFamily;

namespace BrainNet.Service.Font.Implements;

public class FontServiceProvider : IFontServiceProvider
{
    private readonly FontCollection _collection = new();
    private readonly Dictionary<FontFamily, SixLabors.Fonts.FontFamily> _fontFamilies = new();

    public FontServiceProvider()
    {
        using var black = GetFontStream(EmbeddedFont.Roboto_Black);
        using var bold = GetFontStream(EmbeddedFont.Roboto_Bold);
        using var italic = GetFontStream(EmbeddedFont.Roboto_Italic);
        using var light = GetFontStream(EmbeddedFont.Roboto_Light);
        using var regular = GetFontStream(EmbeddedFont.Roboto_Regular);

        using var medium = GetFontStream(EmbeddedFont.Roboto_Medium);
        using var thin = GetFontStream(EmbeddedFont.Roboto_Thin);
        using var blackItalic = GetFontStream(EmbeddedFont.Roboto_BlackItalic);

        using var boldItalic = GetFontStream(EmbeddedFont.Roboto_BoldItalic);
        using var lightItalic = GetFontStream(EmbeddedFont.Roboto_LightItalic);
        using var mediumItalic = GetFontStream(EmbeddedFont.Roboto_MediumItalic);
        using var thinItalic = GetFontStream(EmbeddedFont.Roboto_ThinItalic);
        _fontFamilies.Add(FontFamily.RobotoBlack, _collection.Add(black));
        _fontFamilies.Add(FontFamily.RobotoBold, _collection.Add(bold));
        _fontFamilies.Add(FontFamily.RobotoItalic, _collection.Add(italic));
        _fontFamilies.Add(FontFamily.RobotoLight, _collection.Add(light));
        _fontFamilies.Add(FontFamily.RobotoRegular, _collection.Add(regular));
        _fontFamilies.Add(FontFamily.RobotoMedium, _collection.Add(medium));
        _fontFamilies.Add(FontFamily.RobotoThin, _collection.Add(thin));
        _fontFamilies.Add(FontFamily.RobotoBlackItalic, _collection.Add(blackItalic));
        _fontFamilies.Add(FontFamily.RobotoBoldItalic, _collection.Add(boldItalic));
        _fontFamilies.Add(FontFamily.RobotoLightItalic, _collection.Add(lightItalic));
        _fontFamilies.Add(FontFamily.RobotoMediumItalic, _collection.Add(mediumItalic));
        _fontFamilies.Add(FontFamily.RobotoThinItalic, _collection.Add(thinItalic));
    }

    private MemoryStream GetFontStream(byte[] fontData)
    {
        var stream = new MemoryStream(fontData);
        return stream;
    }

    public SixLabors.Fonts.Font CreateFont(FontFamily family, float size, FontStyle style)
    {
        return _fontFamilies[family].CreateFont(size, style);
    }

    public System.Drawing.Font CreateFont(FontFamily family, float size, System.Drawing.FontStyle style)
    {
        throw new NotImplementedException();
    }
}