using KVYS.Education.Domain.Entities;
using KVYS.Education.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KVYS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class FacultiesController : ControllerBase
{
    private readonly EducationDbContext _context;

    public FacultiesController(EducationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var faculties = await _context.Faculties
            .Where(f => f.IsActive)
            .OrderBy(f => f.Name)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Code,
                DepartmentCount = f.Departments.Count(d => d.IsActive)
            })
            .ToListAsync();

        return Ok(faculties);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var faculty = await _context.Faculties
            .Include(f => f.Departments.Where(d => d.IsActive))
            .FirstOrDefaultAsync(f => f.Id == id);

        if (faculty == null)
            return NotFound();

        return Ok(new
        {
            faculty.Id,
            faculty.Name,
            faculty.Code,
            faculty.IsActive,
            Departments = faculty.Departments.OrderBy(d => d.Name).Select(d => new
            {
                d.Id,
                d.Name,
                d.Code
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFacultyRequest request)
    {
        var faculty = new Faculty(request.Name, request.Code);
        _context.Faculties.Add(faculty);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = faculty.Id }, new { faculty.Id });
    }

    [HttpGet("{id}/departments")]
    public async Task<IActionResult> GetDepartments(Guid id)
    {
        var faculty = await _context.Faculties
            .Include(f => f.Departments.Where(d => d.IsActive))
                .ThenInclude(d => d.Programs.Where(p => p.IsActive))
            .FirstOrDefaultAsync(f => f.Id == id);

        if (faculty == null)
            return NotFound();

        var departments = faculty.Departments
            .OrderBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Code,
                ProgramCount = d.Programs.Count
            });

        return Ok(departments);
    }

    [HttpPost("{id}/departments")]
    public async Task<IActionResult> AddDepartment(Guid id, [FromBody] CreateDepartmentRequest request)
    {
        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty == null)
            return NotFound();

        var department = new Department(id, request.Name, request.Code);
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return Created("", new { department.Id });
    }
}

public record CreateFacultyRequest(string Name, string Code);
public record CreateDepartmentRequest(string Name, string Code);
