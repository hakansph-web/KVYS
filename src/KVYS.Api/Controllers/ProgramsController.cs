using KVYS.Education.Domain.Enums;
using KVYS.Education.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcademicProgram = KVYS.Education.Domain.Entities.Program;

namespace KVYS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProgramsController : ControllerBase
{
    private readonly EducationDbContext _context;

    public ProgramsController(EducationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? departmentId = null)
    {
        var query = _context.Programs
            .Include(p => p.Department)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(p => p.DepartmentId == departmentId.Value);

        var programs = await query
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                p.Level,
                p.AccreditationStatus,
                p.IsActive,
                Department = new { p.Department.Id, p.Department.Name, p.Department.Code }
            })
            .ToListAsync();

        return Ok(programs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var program = await _context.Programs
            .Include(p => p.Department)
            .Include(p => p.ProgramOutcomes.Where(po => po.IsActive))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (program == null)
            return NotFound();

        return Ok(new
        {
            program.Id,
            program.Name,
            program.Code,
            program.Level,
            program.AccreditationStatus,
            program.IsActive,
            Department = new { program.Department.Id, program.Department.Name, program.Department.Code },
            ProgramOutcomes = program.ProgramOutcomes.OrderBy(po => po.SortOrder).Select(po => new
            {
                po.Id,
                po.Code,
                po.Description,
                po.Category,
                po.SortOrder
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProgramRequest request)
    {
        var department = await _context.Departments.FindAsync(request.DepartmentId);
        if (department == null)
            return BadRequest(new { error = "Department not found" });

        var program = new AcademicProgram(
            request.DepartmentId,
            request.Name,
            request.Code,
            request.Level
        );

        _context.Programs.Add(program);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = program.Id }, new { program.Id });
    }

    [HttpGet("{id}/outcomes")]
    public async Task<IActionResult> GetProgramOutcomes(Guid id)
    {
        var program = await _context.Programs
            .Include(p => p.ProgramOutcomes.Where(po => po.IsActive))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (program == null)
            return NotFound();

        var outcomes = program.ProgramOutcomes
            .OrderBy(po => po.SortOrder)
            .Select(po => new
            {
                po.Id,
                po.Code,
                po.Description,
                po.Category,
                po.SortOrder
            });

        return Ok(outcomes);
    }

    [HttpPost("{id}/outcomes")]
    public async Task<IActionResult> AddProgramOutcome(Guid id, [FromBody] CreateProgramOutcomeRequest request)
    {
        var program = await _context.Programs
            .Include(p => p.ProgramOutcomes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (program == null)
            return NotFound();

        var outcome = program.AddProgramOutcome(request.Code, request.Description, request.Category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProgramOutcomes), new { id }, new { outcome.Id });
    }
}

public record CreateProgramRequest(Guid DepartmentId, string Name, string Code, ProgramLevel Level);
public record CreateProgramOutcomeRequest(string Code, string Description, string? Category);
