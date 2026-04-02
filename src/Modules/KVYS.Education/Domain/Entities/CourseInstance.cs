using KVYS.Education.Domain.Enums;
using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Course instance for a specific academic term.
/// </summary>
public class CourseInstance : AggregateRoot
{
    private readonly List<LearningOutcome> _learningOutcomes = [];
    private readonly List<Exam> _exams = [];

    private CourseInstance() { }

    public CourseInstance(Guid courseId, string academicYear, Semester semester, Guid? instructorId = null)
        : base(Guid.NewGuid())
    {
        CourseId = courseId;
        AcademicYear = academicYear;
        Semester = semester;
        InstructorId = instructorId;
    }

    public Guid CourseId { get; private set; }
    public string AcademicYear { get; private set; } = string.Empty;
    public Semester Semester { get; private set; }
    public Guid? InstructorId { get; private set; }
    public string Status { get; private set; } = "Active";

    public virtual Course Course { get; private set; } = null!;
    public IReadOnlyCollection<LearningOutcome> LearningOutcomes => _learningOutcomes.AsReadOnly();
    public IReadOnlyCollection<Exam> Exams => _exams.AsReadOnly();

    public string FullName => $"{AcademicYear} {Semester}";

    public void AssignInstructor(Guid instructorId)
    {
        InstructorId = instructorId;
        SetUpdatedAt();
    }

    public LearningOutcome AddLearningOutcome(string code, string description, BloomLevel? bloomLevel = null)
    {
        var sortOrder = _learningOutcomes.Count + 1;
        var outcome = new LearningOutcome(Id, code, description, bloomLevel, sortOrder);
        _learningOutcomes.Add(outcome);
        SetUpdatedAt();
        return outcome;
    }

    public Exam AddExam(string name, ExamType type, decimal weight, DateOnly? date = null)
    {
        var exam = new Exam(Id, name, type, weight, date);
        _exams.Add(exam);
        SetUpdatedAt();
        return exam;
    }

    public void Close()
    {
        Status = "Closed";
        SetUpdatedAt();
    }
}
