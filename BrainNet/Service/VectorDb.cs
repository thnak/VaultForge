using System.Diagnostics.CodeAnalysis;
using BrainNet.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace BrainNet.Service;

[Experimental("SKEXP0020")]
public class VectorDb
{
    private InMemoryVectorStore vectorStore { get; set; }
    private IVectorStoreRecordCollection<int, Movie> movies { get; set; }
    public VectorDb()
    {
        vectorStore = new InMemoryVectorStore();
        movies = vectorStore.GetCollection<int, Movie>("movies");
    }

    public Task AddMovie(Movie movie, CancellationToken cancellationToken = default)
    {
        return movies.UpsertAsync(movie, null, cancellationToken);
    }
    
    

    public async Task Init()
    {
        await movies.CreateCollectionIfNotExistsAsync();
    }
}