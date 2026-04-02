using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Course definition (template, not semester-specific).
/// </summary>
public class Course : AggregateRoot
{
    private readonly List<CourseInstance> _instances = [];

    private Course() { }

    public Course(
        Guid programId,
        string code,
        string name,
        int credits,
        int theoryHours,
        int practiceHours,
        int semester,
        bool isElective = false)
        : base(Guid.NewGuid())
    {
        ProgramId = programId;
        Code = code;
        Name = name;
        Credits = credits;
        TheoryHours = theoryHours;
        PracticeHours = practiceHours;
        Semester = semester;
        IsElective = isElective;
    }

    public Guid ProgramId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Credits { get; private set; }
    public int TheoryHours { get; private set; }
    public int PracticeHours { get; private set; }
    public int Semester { get; private set; }
    public bool IsElective { get; private set; }
    public bool IsActive { get; private set; } = true;

    public virtual Program Program { get; private set; } = null!;
    public IReadOnlyCollection<CourseInstance> Instances => _instances.AsReadOnly();

    public void Update(string code, string name, int credits, int theoryHours, int practiceHours, int semester)
    {
        Code = code;
        Name = name;
        Credits = credits;
        TheoryHours = theoryHours;
        PracticeHours = practiceHours;
        Semester = semester;
        SetUpdatedAt();
    }

    public CourseInstance CreateInstance(string academicYear, Enums.Semester semester, Guid? instructorId = null)
    {
        var instance = new CourseInstance(Id, academicYear, semester, instructorId);
        _instances.Add(instance);
        return instance;
    }
}
