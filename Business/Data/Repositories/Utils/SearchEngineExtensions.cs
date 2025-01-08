using System.Text;
using BusinessModels.System.InternetOfThings;
using Lucene.Net.Documents;

namespace Business.Data.Repositories.Utils;

public static class SearchEngineExtensions
{
    private static readonly char[] ReservedCharacters =
    {
        '+', '-', '&', '|', '!', '(', ')', '{', '}', '[', ']', '^', '"', '~', '*', '?', ':', '\\', '/'
    };


    /// <summary>
    /// Escapes all reserved characters in a string for safe usage in Lucene queries.
    /// </summary>
    /// <param name="input">The raw query string to escape.</param>
    /// <returns>A string with reserved characters escaped.</returns>
    public static string EscapeLuceneQuery(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var builder = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            if (Array.Exists(ReservedCharacters, reserved => reserved == c))
            {
                builder.Append('\\'); // Add the escape character
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    public static Document IoTDeviceDocumentMapper(this IoTDevice arg)
    {
        var doc = new Document();
        doc.Add(new StringField(nameof(IoTDevice.Id), arg.GetHashCode().ToString(), Field.Store.YES));
        doc.Add(new StringField(nameof(IoTDevice.DeviceName), arg.DeviceName, Field.Store.YES));
        doc.Add(new StringField(nameof(IoTDevice.Location), arg.Location, Field.Store.YES));
        doc.Add(new StringField(nameof(IoTDevice.MacAddress), arg.MacAddress, Field.Store.YES));
        doc.Add(new StringField(nameof(IoTDevice.IpAddress), arg.IpAddress, Field.Store.YES));
        doc.Add(new StringField(nameof(IoTDevice.Manufacturer), arg.Manufacturer, Field.Store.YES));

        return doc;
    }
}