namespace BusinessModels.WebContent;

public static class MimeTypeNames
{
    public static class Application
    {
        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in URL encoded format.</summary>
        public const string FormUrlEncoded = "application/x-www-form-urlencoded";

        public const string GZip = "application/gzip";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in JSON format.</summary>
        public const string Json = MediaTypeNames.Application.Json;

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in JSON patch format.</summary>
        public const string JsonPatch = "application/json-patch+json";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in JSON text sequence format.</summary>
        public const string JsonSequence = "application/json-seq";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in Web Application Manifest.</summary>
        public const string Manifest = "application/manifest+json";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is not interpreted.</summary>
        public const string Octet = "application/octet-stream";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in Portable Document Format (PDF).</summary>
        public const string Pdf = "application/pdf";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in JSON problem detail format.</summary>
        public const string ProblemJson = "application/problem+json";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in XML problem detail format.</summary>
        public const string ProblemXml = "application/problem+xml";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in Rich Text Format (RTF).</summary>
        public const string Rtf = "application/rtf";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is a SOAP document.</summary>
        public const string Soap = "application/soap+xml";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in WASM format.</summary>
        public const string Wasm = "application/wasm";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in XML format.</summary>
        public const string Xml = "application/xml";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in XML Document Type Definition format.</summary>
        public const string XmlDtd = "application/xml-dtd";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is in XML patch format.</summary>
        public const string XmlPatch = "application/xml-patch+xml";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Application" /> data is compressed.</summary>
        public const string Zip = "application/zip";
    }

    /// <summary>Specifies the kind of font data in an email message attachment.</summary>
    public static class Font
    {
        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Font" /> data is in font type collection format.</summary>
        public const string Collection = "font/collection";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Font" /> data is in OpenType Layout (OTF) format.</summary>
        public const string Otf = "font/otf";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Font" /> data is in SFNT format.</summary>
        public const string Sfnt = "font/sfnt";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Font" /> data is in TrueType font (TTF) format.</summary>
        public const string Ttf = "font/ttf";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Font" /> data is in WOFF format.</summary>
        public const string Woff = "font/woff";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Font" /> data is in WOFF2 format.</summary>
        public const string Woff2 = "font/woff2";
    }

    /// <summary>Specifies the type of image data in an email message attachment.</summary>
    public static class Image
    {
        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Image" /> data is in AVIF format.</summary>
        public const string Avif = "image/avif";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Image" /> data is in BMP format.</summary>
        public const string Bmp = "image/bmp";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Image" /> data is in Graphics Interchange Format (GIF).</summary>
        public const string Gif = "image/gif";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Image" /> data is in ICO format.</summary>
        public const string Icon = "image/x-icon";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Image" /> data is in Joint Photographic Experts Group (JPEG) format.</summary>
        public const string Jpeg = "image/jpeg";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Image" /> data is in PNG format.</summary>
        public const string Png = "image/png";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Image" /> data is in SVG format.</summary>
        public const string Svg = "image/svg+xml";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Image" /> data is in Tagged Image File Format (TIFF).</summary>
        public const string Tiff = "image/tiff";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Image" /> data is in WEBP format.</summary>
        public const string Webp = "image/webp";
    }

    public static class Video
    {
        public const string Mp4 = "video/mp4";
        public const string Mkv = "video/x-matroska";
        public const string Avi = "video/x-msvideo";
        public const string Mpeg = "video/mpeg";
        public const string Mpg = "video/mpeg";
        public const string Ts = "video/mp2t";
    }

    public static class Audio
    {
        public const string Mp3 = "audio/mpeg";
        public const string Was = "audio/wav";
        public const string Ogg = "audio/ogg";
        public const string Flac = "audio/flac";
    }

    /// <summary>Specifies the kind of multipart data in an email message attachment.</summary>
    public static class Multipart
    {
        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Multipart" /> data consists of multiple byte ranges.</summary>
        public const string ByteRanges = "multipart/byteranges";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Multipart" /> data is in  format.</summary>
        public const string FormData = "multipart/form-data";

        public const string Mixed = "multipart/mixed";
        public const string Related = "multipart/related";
    }

    /// <summary>Specifies the type of text data in an email message attachment.</summary>
    public static class Text
    {
        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Text" /> data is in CSS format.</summary>
        public const string Css = "text/css";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Text" /> data is in CSV format.</summary>
        public const string Csv = "text/csv";

        public const string EventStream = "text/event-stream";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Text" /> data is in HTML format.</summary>
        public const string Html = "text/html";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Text" /> data is in Javascript format.</summary>
        public const string JavaScript = "text/javascript";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Text" /> data is in Markdown format.</summary>
        public const string Markdown = "text/markdown";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Text" /> data is in plain text format.</summary>
        public const string Plain = "text/plain";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Text" /> data is in Rich Text Format (RTF).</summary>
        public const string RichText = "text/richtext";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Text" /> data is in Rich Text Format (RTF).</summary>
        public const string Rtf = "text/rtf";

        /// <summary>Specifies that the <see cref="T:System.Net.Mime.MediaTypeNames.Text" /> data is in XML format.</summary>
        public const string Xml = "text/xml";
    }
}