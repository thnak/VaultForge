using System.Collections.Concurrent;
using BusinessModels.System.InternetOfThings;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using MongoDB.Bson;

namespace Business.Data.Repositories.InternetOfThings;

public class ThreadSafeDeviceSearchEngine
{
    private readonly FSDirectory _indexDirectory;
    private readonly Analyzer _analyzer;
    private readonly IndexReader _indexReader;
    private readonly IndexSearcher _searcher;
    private readonly QueryParser _queryParser;

    // Use a thread-safe dictionary to store devices (if needed)
    private readonly ConcurrentDictionary<ObjectId, IoTDevice> _devices;

    public ThreadSafeDeviceSearchEngine(string indexPath)
    {
        _indexDirectory = FSDirectory.Open(indexPath);
        Lucene.Net.Store.Directory indexDirectory = FSDirectory.Open(indexPath);
        _analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
        _indexReader = IndexReader.Open(indexDirectory, true);
        _searcher = new IndexSearcher(_indexReader);
        _queryParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "name", _analyzer);
        _devices = new ConcurrentDictionary<ObjectId, IoTDevice>();

        // Load devices and index them (this can be done in a separate thread)
    }

    private void LoadAndIndexDevices(IEnumerable<IoTDevice> devices)
    {
        // Load devices from your data source
        using var writer = new IndexWriter(_indexDirectory, _analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
        foreach (var device in devices)
        {
            var doc = new Document();
            doc.Add(new Field("id", device.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("name", device.DeviceName, Field.Store.YES, Field.Index.ANALYZED));

            writer.AddDocument(doc);
            _devices[device.Id] = device; // Store devices in the dictionary
        }
    }

    public IEnumerable<IoTDevice> Search(string query, int limit = 10)
    {
        var hits = _searcher.Search(_queryParser.Parse(query), limit);

        foreach (var hit in hits.ScoreDocs)
        {
            var doc = _searcher.Doc(hit.Doc);
            var deviceId = ObjectId.Parse(doc.Get("id"));
            if (_devices.TryGetValue(deviceId, out var device))
            {
                yield return device;
            }
        }
    }
    
    public void RemoveDeviceFromIndex(ObjectId deviceId)
    {
        using (var writer = new IndexWriter(_indexDirectory, _analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
        {
            writer.DeleteDocuments(new Term("id", deviceId.ToString()));
            writer.Commit();
        }

        _devices.TryRemove(deviceId, out _); // Remove from in-memory dictionary
    }

    // Dispose of resources when the object is no longer needed
    public void Dispose()
    {
        _indexReader.Dispose();
        _searcher.Dispose();
        _indexDirectory.Dispose();
    }
}