using System.Linq.Expressions;
using BusinessModels.General.Results;
using BusinessModels.System.FileSystem;

namespace Business.Data.Interfaces.FileSystem;

public interface IFolderSystemDatalayer : IMongoDataInitializer, IDataLayerRepository<FolderInfoModel>
{
    FolderInfoModel? Get(string username, string absolute);
    public (FolderInfoModel?, string) GetWithPassword(string id, string password);
    IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(string id, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch);
    IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(Expression<Func<FolderInfoModel, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch);

    public (bool, string, string) CreateFolder(FolderInfoModel folderInfoModel);
    public string GetParentFolder(string id);
    public FolderResult OpenFolder(string id);
    public (bool, string) ChangeFolderPassword(string id, string password);
}