using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// Department within a faculty.
/// </summary>
public class Department : AggregateRoot
{
    private readonly List<Program> _programs = [];

    private Department() { }

    public Department(Guid facultyId, string name, string code)
        : base(Guid.NewGuid())
    {
        FacultyId = facultyId;
        Name = name;
        Code = code;
    }

    public Guid FacultyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public virtual Faculty Faculty { get; private set; } = null!;
    public IReadOnlyCollection<Program> Programs => _programs.AsReadOnly();

    public void Update(string name, string code)
    {
        Name = name;
        Code = code;
        SetUpdatedAt();
    }
}
