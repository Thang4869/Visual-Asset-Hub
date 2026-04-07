using System.Reflection;
using VAH.Backend.Models;
using System;

namespace VAH.Backend.Tests.Builders;

/// <summary>
/// Builder for creating Asset instances in tests.
/// Since Asset constructors are internal, this uses reflection to create proper instances.
/// </summary>
public class AssetBuilder
{
    private int _id = 1;
    private string _fileName = "test-file.jpg";
    private string _filePath = "/uploads/test-file.jpg";
    private AssetContentType _contentType = AssetContentType.Image;
    private int _collectionId = 1;
    private string _userId = "test-user";
    private int? _parentFolderId = null;
    private int? _groupId = null;
    private int _sortOrder = 0;
    private double _positionX = 0;
    private double _positionY = 0;
    private string? _hexCode = null;
    private string? _url = null;

    public AssetBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public AssetBuilder WithName(string name)
    {
        _fileName = name;
        return this;
    }

    public AssetBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public AssetBuilder WithFilePath(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    public AssetBuilder WithFileType(string mimeType)
    {
        _contentType = mimeType.ToLower() switch
        {
            "image/jpeg" or "image/png" or "image/webp" or "image/gif" => AssetContentType.Image,
            var type when type.StartsWith("image/") => AssetContentType.Image,
            _ => AssetContentType.File
        };
        return this;
    }

    public AssetBuilder WithContentType(AssetContentType contentType)
    {
        _contentType = contentType;
        return this;
    }

    public AssetBuilder WithCollectionId(int collectionId)
    {
        _collectionId = collectionId;
        return this;
    }

    public AssetBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public AssetBuilder WithPosition(double x, double y)
    {
        _positionX = x;
        _positionY = y;
        return this;
    }

    public AssetBuilder AsColor(string hexCode = "#FF0000")
    {
        _contentType = AssetContentType.Color;
        _hexCode = hexCode;
        _fileName = hexCode;
        _filePath = hexCode;
        return this;
    }

    public AssetBuilder AsLink(string url)
    {
        _contentType = AssetContentType.Link;
        _url = url;
        _fileName = url;
        _filePath = url;
        return this;
    }

    public AssetBuilder AsPublic()
    {
        // Asset model doesn't have IsPublic - this is a no-op for test compatibility
        return this;
    }

    /// <summary>
    /// Builds an Asset instance. Creates the appropriate subtype and uses reflection 
    /// to set private properties that can't be set via constructor.
    /// </summary>
    public Asset Build()
    {
        Asset asset = _contentType switch
        {
            AssetContentType.Image => CreateImageAsset(),
            AssetContentType.Color => CreateColorAsset(),  
            AssetContentType.Link => CreateLinkAsset(),
            _ => CreateFileAsset()
        };

        // Set ID via reflection (private setter)
        SetProperty(asset, "Id", _id);
        
        // Set position if needed
        if (_positionX != 0 || _positionY != 0)
        {
            SetProperty(asset, "PositionX", _positionX);
            SetProperty(asset, "PositionY", _positionY);
        }

        return asset;
    }

    private ImageAsset CreateImageAsset()
    {
        // Use parameterless constructor then set properties via reflection
        var asset = (ImageAsset)Activator.CreateInstance(typeof(ImageAsset), true)!;
        
        SetProperty(asset, "FileName", _fileName);
        SetProperty(asset, "FilePath", _filePath);
        SetProperty(asset, "ContentType", _contentType);
        SetProperty(asset, "CollectionId", _collectionId);
        SetProperty(asset, "UserId", _userId);
        SetProperty(asset, "ParentFolderId", _parentFolderId);
        SetProperty(asset, "GroupId", _groupId);
        SetProperty(asset, "SortOrder", _sortOrder);
        SetProperty(asset, "CreatedAt", DateTime.UtcNow);
        
        return asset;
    }

    private FileAsset CreateFileAsset()
    {
        var asset = (FileAsset)Activator.CreateInstance(typeof(FileAsset), true)!;
        
        SetProperty(asset, "FileName", _fileName);
        SetProperty(asset, "FilePath", _filePath);
        SetProperty(asset, "ContentType", _contentType);
        SetProperty(asset, "CollectionId", _collectionId);
        SetProperty(asset, "UserId", _userId);
        SetProperty(asset, "ParentFolderId", _parentFolderId);
        SetProperty(asset, "CreatedAt", DateTime.UtcNow);
        
        return asset;
    }

    private ColorAsset CreateColorAsset()
    {
        var asset = (ColorAsset)Activator.CreateInstance(typeof(ColorAsset), true)!;
        var hexCode = _hexCode ?? "#FF0000";
        
        SetProperty(asset, "FileName", hexCode);
        SetProperty(asset, "FilePath", hexCode);
        SetProperty(asset, "ContentType", AssetContentType.Color);
        SetProperty(asset, "CollectionId", _collectionId);
        SetProperty(asset, "UserId", _userId);
        SetProperty(asset, "GroupId", _groupId);
        SetProperty(asset, "HexCode", hexCode);
        SetProperty(asset, "CreatedAt", DateTime.UtcNow);
        
        return asset;
    }

    private LinkAsset CreateLinkAsset()
    {
        var asset = (LinkAsset)Activator.CreateInstance(typeof(LinkAsset), true)!;
        var url = _url ?? _filePath;
        
        SetProperty(asset, "FileName", _fileName);
        SetProperty(asset, "FilePath", url);
        SetProperty(asset, "ContentType", AssetContentType.Link);
        SetProperty(asset, "CollectionId", _collectionId);
        SetProperty(asset, "UserId", _userId);
        SetProperty(asset, "Url", url);
        SetProperty(asset, "CreatedAt", DateTime.UtcNow);
        
        return asset;
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        Type? currentType = obj.GetType();
        while (currentType != null)
        {
            var property = currentType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (property != null)
            {
                if (property.CanWrite)
                {
                    property.SetValue(obj, value);
                    return;
                }
                
                // CanWrite is false, try backing field
                var fieldName = "<" + propertyName + ">k__BackingField";
                var field = currentType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }
            }
            currentType = currentType.BaseType;
        }
    }
}
