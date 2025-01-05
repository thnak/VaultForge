using System.Collections.Concurrent;
using Business.Data.Interfaces;
using Business.Data.Repositories.Utils;
using BusinessModels.Base;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = System.IO.Directory;

namespace Business.Data.Repositories;

public class ThreadSafeSearchEngine<T> : IThreadSafeSearchEngine<T> where T : BaseModelEntry
{
    private readonly RAMDirectory _indexDirectory;
    private readonly MultiFieldQueryParser _queryParser;
    private readonly Analyzer _analyzer;
    private IndexSearcher? Searcher { get; set; }
    private readonly ConcurrentDictionary<int, T> _items;
    private readonly Func<T, Document> _documentMapper;
    private const string DocumentIdFieldName = nameof(BaseModelEntry.Id);
    private const LuceneVersion Version = LuceneVersion.LUCENE_48;
    private readonly IndexWriter _writer;

    public ThreadSafeSearchEngine(string indexPath, Func<T, Document> documentMapper)
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        indexPath = Path.Combine(basePath, indexPath);
        Directory.CreateDirectory(indexPath);
        _indexDirectory = new RAMDirectory();
        
        _analyzer = new StandardAnalyzer(Version);

        T model = Activator.CreateInstance<T>();
        var mappedValue = documentMapper(model);
        IEnumerable<string> fields = mappedValue.Fields.Select(x => x.Name);

        _queryParser = new MultiFieldQueryParser(Version, fields.ToArray(), _analyzer);

        _items = new ConcurrentDictionary<int, T>();
        _documentMapper = documentMapper;

        var indexConfig = new IndexWriterConfig(Version, _analyzer);
        _writer = new IndexWriter(_indexDirectory, indexConfig);
        Searcher = new IndexSearcher(_writer.GetReader(applyAllDeletes: true));
    }

    public void LoadAndIndexItems(IEnumerable<T> items)
    {
        var indexConfig = new IndexWriterConfig(Version, _analyzer);
        using var writer = new IndexWriter(_indexDirectory, indexConfig);
        foreach (var item in items)
        {
            var doc = _documentMapper(item);
            writer.AddDocument(doc);
            var itemHash = item.GetHashCode();
            _items.AddOrUpdate(itemHash, item, (_, _) => item);
        }

        writer.Commit();
        _writer.Flush(triggerMerge: false, applyAllDeletes: false);
    }

    public async Task LoadAndIndexItems(IAsyncEnumerable<T> items)
    {
        await foreach (var item in items)
        {
            var doc = _documentMapper(item);
            _writer.AddDocument(doc);
            var itemHash = item.GetHashCode();
            _items.AddOrUpdate(itemHash, item, (_, _) => item);
        }

        _writer.Commit();
        _writer.Flush(triggerMerge: false, applyAllDeletes: false);
    }


    public IEnumerable<T> Search(string query, int limit = 10)
    {
        if (Searcher != null)
        {
            var hits = Searcher.Search(_queryParser.Parse(query.EscapeLuceneQuery()), limit);
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
        _writer.DeleteDocuments(new Term(DocumentIdFieldName, item.GetHashCode().ToString()));
        _writer.Commit();


        _items.TryRemove(item.GetHashCode(), out _);
    }

    // Dispose of resources when the object is no longer needed
    public void Dispose()
    {
        _indexDirectory.Dispose();
        _writer.Dispose();
    }
}