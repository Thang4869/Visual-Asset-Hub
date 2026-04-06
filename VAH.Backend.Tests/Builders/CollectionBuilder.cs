using VAH.Backend.Models;

namespace VAH.Backend.Tests.Builders;

/// <summary>
/// Builder for creating Collection instances in tests.
/// </summary>
public class CollectionBuilder
{
    private int _id = 1;
    private string _name = "Test Collection";
    private string _description = "Test Description";
    private int? _parentId = null;
    private DateTime _createdAt = DateTime.UtcNow;
    private string _color = "#007bff";
    private CollectionType _type = CollectionType.Default;
    private int _order = 0;
    private LayoutType _layoutType = LayoutType.Grid;
    private string? _userId = "test-user";

    public CollectionBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public CollectionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CollectionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CollectionBuilder WithParentId(int? parentId)
    {
        _parentId = parentId;
        return this;
    }

    public CollectionBuilder WithUserId(string? userId)
    {
        _userId = userId;
        return this;
    }

    public CollectionBuilder WithColor(string color)
    {
        _color = color;
        return this;
    }

    public CollectionBuilder WithType(CollectionType type)
    {
        _type = type;
        return this;
    }

    public CollectionBuilder WithOrder(int order)
    {
        _order = order;
        return this;
    }

    public CollectionBuilder WithLayoutType(LayoutType layoutType)
    {
        _layoutType = layoutType;
        return this;
    }

    public CollectionBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    /// <summary>
    /// Builds a Collection instance with all configured properties.
    /// </summary>
    public Collection Build()
    {
        return new Collection
        {
            Id = _id,
            Name = _name,
            Description = _description,
            ParentId = _parentId,
            CreatedAt = _createdAt,
            Color = _color,
            Type = _type,
            Order = _order,
            LayoutType = _layoutType,
            UserId = _userId
        };
    }
}