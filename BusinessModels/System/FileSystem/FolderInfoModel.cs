using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

public class FolderInfoModel
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>
    /// Name of the folder
    /// </summary>
    public string FolderName { get; set; } = string.Empty;
    
    /// <summary>
    /// List of content in the folder, include their child folder
    /// </summary>
    public List<FolderContent> Contents { get; set; } = [];

    /// <summary>
    /// The one that own this folder
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Share this folder to other user
    /// </summary>
    public List<string> SharedTo { get; set; } = [];

    /// <summary>
    /// Folder password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Last modified date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Relative path for UI display
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    public string VersionId { get; set; } = string.Empty;
    
    #region Front-End Properties

    [BsonIgnore] public long FolderSize { get; set; }

    #endregion

    #region Front-End Methods

    public override bool Equals(object? o)
    {
        var other = o as FileInfoModel;
        return other?.Id == Id;
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => Id.ToString().GetHashCode();


    public override string ToString()
    {
        return FolderName;
    }

    #endregion
}