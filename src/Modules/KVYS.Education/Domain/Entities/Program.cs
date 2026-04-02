using KVYS.Education.Domain.Enums;
using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Academic program (e.g., Computer Engineering BSc).
/// </summary>
public class Program : AggregateRoot
{
    private readonly List<ProgramOutcome> _programOutcomes = [];
    private readonly List<Course> _courses = [];

    private Program() { }

    public Program(Guid departmentId, string name, string code, ProgramLevel level)
        : base(Guid.NewGuid())
    {
        DepartmentId = departmentId;
        Name = name;
        Code = code;
        Level = level;
    }

    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public ProgramLevel Level { get; private set; }
    public string? AccreditationStatus { get; private set; }
    public bool IsActive { get; private set; } = true;

    public virtual Department Department { get; private set; } = null!;
    public IReadOnlyCollection<ProgramOutcome> ProgramOutcomes => _programOutcomes.AsReadOnly();
    public IReadOnlyCollection<Course> Courses => _courses.AsReadOnly();

    public void Update(string name, string code, ProgramLevel level)
    {
        Name = name;
        Code = code;
        Level = level;
        SetUpdatedAt();
    }

    public void SetAccreditationStatus(string status)
    {
        AccreditationStatus = status;
        SetUpdatedAt();
    }

    public ProgramOutcome AddProgramOutcome(string code, string description, string? category = null)
    {
        var sortOrder = _programOutcomes.Count + 1;
        var outcome = new ProgramOutcome(Id, code, description, category, sortOrder);
        _programOutcomes.Add(outcome);
        SetUpdatedAt();
        return outcome;
    }
}
