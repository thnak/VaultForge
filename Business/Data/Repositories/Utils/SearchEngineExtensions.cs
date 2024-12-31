using BusinessModels.System.InternetOfThings;
using Lucene.Net.Documents;

namespace Business.Data.Repositories.Utils;

public static class SearchEngineExtensions
{
    public static Document IoTDeviceDocumentMapper(this IoTDevice arg)
    {
        var doc = new Document();
        doc.Add(new Field("id", arg.GetHashCode().ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
        doc.Add(new Field("name", arg.DeviceName, Field.Store.YES, Field.Index.ANALYZED));
        doc.Add(new Field("Location", arg.Location, Field.Store.YES, Field.Index.ANALYZED));
        doc.Add(new Field("MacAddress", arg.MacAddress, Field.Store.YES, Field.Index.ANALYZED));
        doc.Add(new Field("IpAddress", arg.IpAddress, Field.Store.YES, Field.Index.ANALYZED));
        return doc;
    }
}