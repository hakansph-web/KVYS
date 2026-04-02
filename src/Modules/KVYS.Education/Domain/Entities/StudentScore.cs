using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Student's score for an exam question.
/// </summary>
public class StudentScore : Entity
{
    private StudentScore() { }

    public StudentScore(Guid questionId, string studentId, decimal score)
        : base(Guid.NewGuid())
    {
        QuestionId = questionId;
        StudentId = studentId;
        Score = score;
    }

    public Guid QuestionId { get; private set; }
    /// <summary>
    /// Anonymized student identifier (not linked to personal data).
    /// </summary>
    public string StudentId { get; private set; } = string.Empty;
    public decimal Score { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public virtual ExamQuestion Question { get; private set; } = null!;

    public void UpdateScore(decimal score)
    {
        Score = score;
    }
}
