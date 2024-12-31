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
    private IndexReader? IndexReader { get; set; }
    private IndexSearcher? Searcher { get; set; }
    private readonly ConcurrentDictionary<int, T> _items;
    private readonly Func<T, Document> _documentMapper;

    public ThreadSafeSearchEngine(string indexPath, Func<T, Document> documentMapper)
    {
        Directory.CreateDirectory(indexPath);
        _indexDirectory = FSDirectory.Open(indexPath);
        _analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

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
            _items.AddOrUpdate(itemHash, item, (_, _) => item);
        }

        writer.Commit();
        InitSearcher();
    }

    public async Task LoadAndIndexItems(IAsyncEnumerable<T> items)
    {
        using var writer = new IndexWriter(_indexDirectory, _analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
        await foreach (var item in items)
        {
            var doc = _documentMapper(item);
            writer.AddDocument(doc);
            var itemHash = item.GetHashCode();
            _items.AddOrUpdate(itemHash, item, (_, _) => item);
        }

        writer.Commit();
        InitSearcher();
    }

    private void InitSearcher()
    {
        if (IndexReader != null)
            IndexReader.Dispose();
        if (Searcher != null)
            Searcher.Dispose();
        IndexReader = IndexReader.Open(_indexDirectory, true);
        Searcher = new IndexSearcher(IndexReader);
    }

    public IEnumerable<T> Search(string query, int limit = 10)
    {
        if (Searcher != null)
        {
            var hits = Searcher.Search(_queryParser.Parse(query), limit);
            foreach (var hit in hits.ScoreDocs)
            {
                var doc = Searcher.Doc(hit.Doc);
                int itemId = int.Parse(doc.Get("id"));
                if (_items.TryGetValue(itemId, out var item))
                {
                    yield return item;
                }
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
        IndexReader?.Dispose();
        Searcher?.Dispose();
        _indexDirectory.Dispose();
    }
}