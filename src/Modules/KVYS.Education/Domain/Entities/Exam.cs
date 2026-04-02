using KVYS.Education.Domain.Enums;
using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Exam for a course instance.
/// </summary>
public class Exam : AggregateRoot
{
    private readonly List<ExamQuestion> _questions = [];

    private Exam() { }

    public Exam(Guid courseInstanceId, string name, ExamType type, decimal weight, DateOnly? date = null)
        : base(Guid.NewGuid())
    {
        CourseInstanceId = courseInstanceId;
        Name = name;
        Type = type;
        Weight = weight;
        Date = date;
    }

    public Guid CourseInstanceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ExamType Type { get; private set; }
    public DateOnly? Date { get; private set; }
    public decimal Weight { get; private set; }
    public decimal? TotalPoints { get; private set; }

    public virtual CourseInstance CourseInstance { get; private set; } = null!;
    public IReadOnlyCollection<ExamQuestion> Questions => _questions.AsReadOnly();

    public void Update(string name, ExamType type, decimal weight, DateOnly? date)
    {
        Name = name;
        Type = type;
        Weight = weight;
        Date = date;
        SetUpdatedAt();
    }

    public ExamQuestion AddQuestion(int questionNumber, decimal points, string? description = null)
    {
        var question = new ExamQuestion(Id, questionNumber, points, description);
        _questions.Add(question);
        RecalculateTotalPoints();
        SetUpdatedAt();
        return question;
    }

    public void RemoveQuestion(Guid questionId)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question != null)
        {
            _questions.Remove(question);
            RecalculateTotalPoints();
            SetUpdatedAt();
        }
    }

    private void RecalculateTotalPoints()
    {
        TotalPoints = _questions.Sum(q => q.Points);
    }
}
