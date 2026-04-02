using KVYS.Education.Domain.Enums;
using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Learning outcome (ÖÇ - Öğrenme Çıktısı) for a course instance.
/// </summary>
public class LearningOutcome : Entity
{
    private readonly List<LearningOutcomeProgramOutcomeMapping> _programOutcomeMappings = [];
    private readonly List<QuestionLearningOutcomeMapping> _questionMappings = [];

    private LearningOutcome() { }

    public LearningOutcome(Guid courseInstanceId, string code, string description, BloomLevel? bloomLevel, int sortOrder)
        : base(Guid.NewGuid())
    {
        CourseInstanceId = courseInstanceId;
        Code = code;
        Description = description;
        BloomLevel = bloomLevel;
        SortOrder = sortOrder;
    }

    public Guid CourseInstanceId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public BloomLevel? BloomLevel { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public virtual CourseInstance CourseInstance { get; private set; } = null!;
    public IReadOnlyCollection<LearningOutcomeProgramOutcomeMapping> ProgramOutcomeMappings
        => _programOutcomeMappings.AsReadOnly();
    public IReadOnlyCollection<QuestionLearningOutcomeMapping> QuestionMappings
        => _questionMappings.AsReadOnly();

    public void Update(string code, string description, BloomLevel? bloomLevel)
    {
        Code = code;
        Description = description;
        BloomLevel = bloomLevel;
    }

    public void MapToProgramOutcome(Guid programOutcomeId, int contributionLevel)
    {
        if (_programOutcomeMappings.Any(m => m.ProgramOutcomeId == programOutcomeId))
            return;

        var mapping = new LearningOutcomeProgramOutcomeMapping(Id, programOutcomeId, contributionLevel);
        _programOutcomeMappings.Add(mapping);
    }

    public void RemoveProgramOutcomeMapping(Guid programOutcomeId)
    {
        var mapping = _programOutcomeMappings.FirstOrDefault(m => m.ProgramOutcomeId == programOutcomeId);
        if (mapping != null)
            _programOutcomeMappings.Remove(mapping);
    }
}
