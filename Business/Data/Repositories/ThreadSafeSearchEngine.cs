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
    private const string DocumentIdFieldName = nameof(BaseModelEntry.Id);
    private const Lucene.Net.Util.Version Version = Lucene.Net.Util.Version.LUCENE_29;

    public ThreadSafeSearchEngine(string indexPath, IEnumerable<string> fields, Func<T, Document> documentMapper)
    {
        Directory.CreateDirectory(indexPath);
        _indexDirectory = FSDirectory.Open(indexPath);
        _analyzer = new StandardAnalyzer(Version);
        _queryParser = new MultiFieldQueryParser(Version, fields.ToArray(), _analyzer);

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
                int itemId = int.Parse(doc.Get(DocumentIdFieldName));
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
            writer.DeleteDocuments(new Term(DocumentIdFieldName, item.GetHashCode().ToString()));
            writer.Commit();
        }

        _items.TryRemove(item.GetHashCode(), out _);
        InitSearcher();
    }

    // Dispose of resources when the object is no longer needed
    public void Dispose()
    {
        IndexReader?.Dispose();
        Searcher?.Dispose();
        _indexDirectory.Dispose();
    }
}