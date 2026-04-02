using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Program outcome (PÇ - Program Çıktısı).
/// </summary>
public class ProgramOutcome : Entity
{
    private readonly List<LearningOutcomeProgramOutcomeMapping> _learningOutcomeMappings = [];

    private ProgramOutcome() { }

    public ProgramOutcome(Guid programId, string code, string description, string? category, int sortOrder)
        : base(Guid.NewGuid())
    {
        ProgramId = programId;
        Code = code;
        Description = description;
        Category = category;
        SortOrder = sortOrder;
    }

    public Guid ProgramId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public virtual Program Program { get; private set; } = null!;
    public IReadOnlyCollection<LearningOutcomeProgramOutcomeMapping> LearningOutcomeMappings
        => _learningOutcomeMappings.AsReadOnly();

    public void Update(string code, string description, string? category)
    {
        Code = code;
        Description = description;
        Category = category;
    }

    public void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
