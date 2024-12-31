using System.Collections.Concurrent;
using Business.Data.Interfaces;
using BusinessModels.Base;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = System.IO.Directory;

namespace Business.Data.Repositories;

public class ThreadSafeSearchEngine<T> : IThreadSafeSearchEngine<T> where T : BaseModelEntry
{
    private readonly FSDirectory _indexDirectory;
    private readonly QueryParser _queryParser;
    private readonly Analyzer _analyzer;
    private readonly IndexReader _indexReader;
    private readonly IndexSearcher _searcher;
    private readonly ConcurrentDictionary<int, T> _items;
    private readonly Func<T, Document> _documentMapper;

    public ThreadSafeSearchEngine(string indexPath, Func<T, Document> documentMapper)
    {
        Directory.CreateDirectory(indexPath);
        _indexDirectory = FSDirectory.Open(indexPath);
        _analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
        _indexReader = IndexReader.Open(_indexDirectory, true);
        _searcher = new IndexSearcher(_indexReader);
        _queryParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "name", _analyzer);
        _items = new ConcurrentDictionary<int, T>();
        _documentMapper = documentMapper;
    }

    public void LoadAndIndexItems(IEnumerable<T> items)
    {
        using var writer = new IndexWriter(_indexDirectory, _analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
        foreach (var item in items)
        {
            var doc = _documentMapper(item);
            writer.AddDocument(doc);
            var itemHash = item.GetHashCode();
            _items.AddOrUpdate(itemHash, item, (key, old) => item);
        }
    }

    public async Task LoadAndIndexItems(IAsyncEnumerable<T> items)
    {
        using var writer = new IndexWriter(_indexDirectory, _analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
        await foreach (var item in items)
        {
            var doc = _documentMapper(item);
            writer.AddDocument(doc);
            var itemHash = item.GetHashCode();
            _items.AddOrUpdate(itemHash, item, (key, old) => item);
        }
    }

    public IEnumerable<T> Search(string query, int limit = 10)
    {
        var hits = _searcher.Search(_queryParser.Parse(query), limit);
        foreach (var hit in hits.ScoreDocs)
        {
            var doc = _searcher.Doc(hit.Doc);
            int itemId = int.Parse(doc.Get("id"));
            if (_items.TryGetValue(itemId, out var item))
            {
                yield return item;
            }
        }
    }

    public void RemoveItemFromIndex(T item)
    {
        using (var writer = new IndexWriter(_indexDirectory, _analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
        {
            writer.DeleteDocuments(new Term("id", item.GetHashCode().ToString()));
            writer.Commit();
        }

        _items.TryRemove(item.GetHashCode(), out _);
    }

    // Dispose of resources when the object is no longer needed
    public void Dispose()
    {
        _indexReader.Dispose();
        _searcher.Dispose();
        _indexDirectory.Dispose();
    }
}