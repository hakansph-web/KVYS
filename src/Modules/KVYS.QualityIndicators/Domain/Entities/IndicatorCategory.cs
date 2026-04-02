using KVYS.Shared.Domain.Primitives;

namespace KVYS.QualityIndicators.Domain.Entities;

/// <summary>
/// Hierarchical category for quality indicators (e.g., A. Leadership, A.1 Quality Assurance).
/// </summary>
public class IndicatorCategory : AggregateRoot
{
    private readonly List<IndicatorCategory> _children = [];
    private readonly List<Indicator> _indicators = [];

    private IndicatorCategory() { }

    public IndicatorCategory(string code, string name, string? description = null, Guid? parentId = null)
        : base(Guid.NewGuid())
    {
        Code = code;
        Name = name;
        Description = description;
        ParentId = parentId;
    }

    public Guid? ParentId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    public virtual IndicatorCategory? Parent { get; private set; }
    public IReadOnlyCollection<IndicatorCategory> Children => _children.AsReadOnly();
    public IReadOnlyCollection<Indicator> Indicators => _indicators.AsReadOnly();

    public void Update(string code, string name, string? description)
    {
        Code = code;
        Name = name;
        Description = description;
        SetUpdatedAt();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
