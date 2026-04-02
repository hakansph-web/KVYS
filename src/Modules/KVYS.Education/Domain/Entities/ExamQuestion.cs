using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Question within an exam.
/// </summary>
public class ExamQuestion : Entity
{
    private readonly List<QuestionLearningOutcomeMapping> _learningOutcomeMappings = [];
    private readonly List<StudentScore> _scores = [];

    private ExamQuestion() { }

    public ExamQuestion(Guid examId, int questionNumber, decimal points, string? description = null)
        : base(Guid.NewGuid())
    {
        ExamId = examId;
        QuestionNumber = questionNumber;
        Points = points;
        Description = description;
    }

    public Guid ExamId { get; private set; }
    public int QuestionNumber { get; private set; }
    public decimal Points { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public virtual Exam Exam { get; private set; } = null!;
    public IReadOnlyCollection<QuestionLearningOutcomeMapping> LearningOutcomeMappings
        => _learningOutcomeMappings.AsReadOnly();
    public IReadOnlyCollection<StudentScore> Scores => _scores.AsReadOnly();

    public void Update(decimal points, string? description)
    {
        Points = points;
        Description = description;
    }

    public void MapToLearningOutcome(Guid learningOutcomeId)
    {
        if (_learningOutcomeMappings.Any(m => m.LearningOutcomeId == learningOutcomeId))
            return;

        var mapping = new QuestionLearningOutcomeMapping(Id, learningOutcomeId);
        _learningOutcomeMappings.Add(mapping);
    }

    public void RemoveLearningOutcomeMapping(Guid learningOutcomeId)
    {
        var mapping = _learningOutcomeMappings.FirstOrDefault(m => m.LearningOutcomeId == learningOutcomeId);
        if (mapping != null)
            _learningOutcomeMappings.Remove(mapping);
    }

    public void AddScore(string studentId, decimal score)
    {
        var existingScore = _scores.FirstOrDefault(s => s.StudentId == studentId);
        if (existingScore != null)
        {
            existingScore.UpdateScore(score);
        }
        else
        {
            _scores.Add(new StudentScore(Id, studentId, score));
        }
    }
}
