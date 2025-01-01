using BusinessModels.System.InternetOfThings;
using Lucene.Net.Documents;

namespace Business.Data.Repositories.Utils;

public static class SearchEngineExtensions
{
    public static Document IoTDeviceDocumentMapper(this IoTDevice arg)
    {
        var doc = new Document();
        doc.Add(new Field(nameof(IoTDevice.Id), arg.GetHashCode().ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
        doc.Add(new Field(nameof(IoTDevice.DeviceName), arg.DeviceName, Field.Store.YES, Field.Index.ANALYZED));
        doc.Add(new Field(nameof(IoTDevice.Location), arg.Location, Field.Store.YES, Field.Index.ANALYZED));
        doc.Add(new Field(nameof(IoTDevice.MacAddress), arg.MacAddress, Field.Store.YES, Field.Index.ANALYZED));
        doc.Add(new Field(nameof(IoTDevice.IpAddress), arg.IpAddress, Field.Store.YES, Field.Index.ANALYZED));
        doc.Add(new Field(nameof(IoTDevice.Manufacturer), arg.Manufacturer, Field.Store.YES, Field.Index.ANALYZED));

        return doc;
    }
}