using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Mapping between learning outcome and program outcome with contribution level.
/// </summary>
public class LearningOutcomeProgramOutcomeMapping : Entity
{
    private LearningOutcomeProgramOutcomeMapping() { }

    public LearningOutcomeProgramOutcomeMapping(Guid learningOutcomeId, Guid programOutcomeId, int contributionLevel)
        : base(Guid.NewGuid())
    {
        LearningOutcomeId = learningOutcomeId;
        ProgramOutcomeId = programOutcomeId;
        ContributionLevel = contributionLevel;
    }

    public Guid LearningOutcomeId { get; private set; }
    public Guid ProgramOutcomeId { get; private set; }
    /// <summary>
    /// Contribution level: 1 = Low, 2 = Medium, 3 = High
    /// </summary>
    public int ContributionLevel { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public virtual LearningOutcome LearningOutcome { get; private set; } = null!;
    public virtual ProgramOutcome ProgramOutcome { get; private set; } = null!;

    public void UpdateContributionLevel(int level)
    {
        if (level < 1 || level > 3)
            throw new ArgumentOutOfRangeException(nameof(level), "Contribution level must be between 1 and 3.");

        ContributionLevel = level;
    }
}
