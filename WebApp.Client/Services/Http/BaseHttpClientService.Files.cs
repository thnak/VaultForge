using System.Security.Claims;
using BusinessModels.General.EnumModel;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using BusinessModels.Utils;
using BusinessModels.WebContent.Drive;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public async Task<ResponseDataResult<FolderRequest>> GetFolderRequestAsync(string? folderId, int currentPage, int pageSize, string? password, bool forceReload, bool isDeletedPage)
    {
        using var formData = new MultipartFormDataContent();
        if (folderId != null)
            formData.Add(new StringContent(folderId), "id");
        formData.Add(new StringContent(currentPage.ToString()), "page");
        formData.Add(new StringContent(pageSize.ToString()), "pageSize");

        if (password != null)
            formData.Add(new StringContent(password), "password");

        if (isDeletedPage)
        {
            FolderContentType[] types = [FolderContentType.DeletedFolder];
            formData.Add(new StringContent(types.ToJson()), "contentTypes");
        }

        formData.Add(new StringContent(forceReload.ToJson()), "forceReLoad");

        var authentication = await PersistentAuthenticationStateService.GetAuthenticationStateAsync();
        string useName = string.Empty;
        if (Navigation.Uri.EndsWith(PageRoutes.Drive.Index.Src))
        {
            useName = authentication.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        formData.Add(new StringContent(useName), "username");
        var responseMessage = await PostAsync<FolderRequest>(isDeletedPage ? "/api/files/get-deleted-content" : $"/api/Files/get-folder", formData);
        return responseMessage;
    }
    
    
}