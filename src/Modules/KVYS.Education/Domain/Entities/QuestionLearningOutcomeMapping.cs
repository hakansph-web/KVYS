using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Mapping between exam question and learning outcome.
/// </summary>
public class QuestionLearningOutcomeMapping : Entity
{
    private QuestionLearningOutcomeMapping() { }

    public QuestionLearningOutcomeMapping(Guid questionId, Guid learningOutcomeId)
        : base(Guid.NewGuid())
    {
        QuestionId = questionId;
        LearningOutcomeId = learningOutcomeId;
    }

    public Guid QuestionId { get; private set; }
    public Guid LearningOutcomeId { get; private set; }

    public virtual ExamQuestion Question { get; private set; } = null!;
    public virtual LearningOutcome LearningOutcome { get; private set; } = null!;
}
