using KVYS.Shared.Domain.Primitives;

namespace KVYS.Education.Domain.Entities;

/// <summary>
/// University faculty/school.
/// </summary>
public class Faculty : AggregateRoot
{
    private readonly List<Department> _departments = [];

    private Faculty() { }

    public Faculty(string name, string code)
        : base(Guid.NewGuid())
    {
        Name = name;
        Code = code;
    }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<Department> Departments => _departments.AsReadOnly();

    public void Update(string name, string code)
    {
        Name = name;
        Code = code;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
