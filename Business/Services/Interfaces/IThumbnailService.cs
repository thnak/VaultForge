namespace Business.Services.Interfaces;

public interface IThumbnailService 
{
    void AddThumbnailRequest(string imageId);
    Task StartAsync(CancellationToken cancellationToken);
}