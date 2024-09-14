namespace Business.Services.Interfaces;

public interface IThumbnailService 
{
    Task AddThumbnailRequest(string imageId);
    Task StartAsync(CancellationToken cancellationToken);
}