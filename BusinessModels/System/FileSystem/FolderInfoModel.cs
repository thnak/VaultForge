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

    public string AbsolutePath { get; set; } = string.Empty; // Absolute path for folder storage

    /// <summary>
    /// List of content in the folder, include their child folder
    /// </summary>
    public List<string> Contents { get; set; } = [];

    /// <summary>
    /// Folder password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Username own this folder
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Last modified date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Relative path for UI display
    /// </summary>
    [BsonIgnore]
    public string RelativePath { get; set; } = string.Empty;

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