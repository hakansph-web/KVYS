using KVYS.QualityIndicators.Domain.Enums;
using KVYS.Shared.Domain.Primitives;

namespace KVYS.QualityIndicators.Domain.Entities;

/// <summary>
/// Quality indicator definition (e.g., Student/Faculty Ratio).
/// </summary>
public class Indicator : AggregateRoot
{
    private readonly List<IndicatorEntry> _entries = [];

    private Indicator() { }

    public Indicator(
        Guid categoryId,
        string code,
        string name,
        IndicatorDataType dataType,
        CollectionFrequency frequency,
        string? description = null,
        string? unit = null,
        string? formula = null)
        : base(Guid.NewGuid())
    {
        CategoryId = categoryId;
        Code = code;
        Name = name;
        DataType = dataType;
        CollectionFrequency = frequency;
        Description = description;
        Unit = unit;
        Formula = formula;
    }

    public Guid CategoryId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public IndicatorDataType DataType { get; private set; }
    public string? Unit { get; private set; }
    public string? Formula { get; private set; }
    public decimal? TargetValue { get; private set; }
    public string? TargetOperator { get; private set; }
    public CollectionFrequency CollectionFrequency { get; private set; }
    public bool IsRequired { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    public virtual IndicatorCategory Category { get; private set; } = null!;
    public IReadOnlyCollection<IndicatorEntry> Entries => _entries.AsReadOnly();

    public void Update(string code, string name, string? description, string? unit, string? formula)
    {
        Code = code;
        Name = name;
        Description = description;
        Unit = unit;
        Formula = formula;
        SetUpdatedAt();
    }

    public void SetTarget(decimal? value, string? operatorSymbol)
    {
        TargetValue = value;
        TargetOperator = operatorSymbol;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public bool EvaluateTarget(decimal value)
    {
        if (!TargetValue.HasValue || string.IsNullOrEmpty(TargetOperator))
            return true;

        return TargetOperator switch
        {
            ">" => value > TargetValue.Value,
            ">=" => value >= TargetValue.Value,
            "<" => value < TargetValue.Value,
            "<=" => value <= TargetValue.Value,
            "=" => value == TargetValue.Value,
            _ => true
        };
    }
}
