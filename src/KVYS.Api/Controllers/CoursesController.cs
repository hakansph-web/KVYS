using KVYS.Education.Domain.Entities;
using KVYS.Education.Domain.Enums;
using KVYS.Education.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KVYS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly EducationDbContext _context;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(EducationDbContext context, ILogger<CoursesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? programId = null)
    {
        var query = _context.Courses
            .Include(c => c.Program)
            .Where(c => c.IsActive)
            .AsQueryable();

        if (programId.HasValue)
            query = query.Where(c => c.ProgramId == programId.Value);

        var courses = await query
            .OrderBy(c => c.Semester)
            .ThenBy(c => c.Code)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.Credits,
                c.TheoryHours,
                c.PracticeHours,
                c.Semester,
                c.IsElective,
                Program = new { c.Program.Id, c.Program.Name, c.Program.Code }
            })
            .ToListAsync();

        return Ok(courses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var course = await _context.Courses
            .Include(c => c.Program)
            .Include(c => c.Instances)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound();

        return Ok(new
        {
            course.Id,
            course.Code,
            course.Name,
            course.Credits,
            course.TheoryHours,
            course.PracticeHours,
            course.Semester,
            course.IsElective,
            course.IsActive,
            Program = new { course.Program.Id, course.Program.Name, course.Program.Code },
            Instances = course.Instances.OrderByDescending(i => i.AcademicYear).Select(i => new
            {
                i.Id,
                i.AcademicYear,
                i.Semester,
                i.Status
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request)
    {
        var program = await _context.Programs.FindAsync(request.ProgramId);
        if (program == null)
            return BadRequest(new { error = "Program not found" });

        var course = new Course(
            request.ProgramId,
            request.Code,
            request.Name,
            request.Credits,
            request.TheoryHours,
            request.PracticeHours,
            request.Semester,
            request.IsElective
        );

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created course: {Code} - {Name}", course.Code, course.Name);
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, new { course.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseRequest request)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
            return NotFound();

        course.Update(
            request.Code,
            request.Name,
            request.Credits,
            request.TheoryHours,
            request.PracticeHours,
            request.Semester
        );

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Course Instances
    [HttpGet("{id}/instances")]
    public async Task<IActionResult> GetInstances(Guid id)
    {
        var course = await _context.Courses
            .Include(c => c.Instances)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound();

        var instances = course.Instances
            .OrderByDescending(i => i.AcademicYear)
            .ThenByDescending(i => i.Semester)
            .Select(i => new
            {
                i.Id,
                i.AcademicYear,
                i.Semester,
                i.InstructorId,
                i.Status
            });

        return Ok(instances);
    }

    [HttpPost("{id}/instances")]
    public async Task<IActionResult> CreateInstance(Guid id, [FromBody] CreateInstanceRequest request)
    {
        var course = await _context.Courses
            .Include(c => c.Instances)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound();

        // Check if instance already exists
        var existingInstance = course.Instances
            .FirstOrDefault(i => i.AcademicYear == request.AcademicYear && i.Semester == request.Semester);

        if (existingInstance != null)
            return BadRequest(new { error = "Instance already exists for this academic year and semester" });

        var instance = new CourseInstance(course.Id, request.AcademicYear, request.Semester, request.InstructorId);
        _context.CourseInstances.Add(instance);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created course instance: {CourseCode} - {Year} {Semester}",
            course.Code, request.AcademicYear, request.Semester);

        return CreatedAtAction(nameof(GetInstance), new { id, instanceId = instance.Id }, new { instance.Id });
    }

    [HttpGet("{id}/instances/{instanceId}")]
    public async Task<IActionResult> GetInstance(Guid id, Guid instanceId)
    {
        var instance = await _context.CourseInstances
            .Include(i => i.Course)
            .Include(i => i.LearningOutcomes)
            .Include(i => i.Exams)
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.CourseId == id);

        if (instance == null)
            return NotFound();

        return Ok(new
        {
            instance.Id,
            instance.AcademicYear,
            instance.Semester,
            instance.InstructorId,
            instance.Status,
            Course = new { instance.Course.Id, instance.Course.Code, instance.Course.Name },
            LearningOutcomes = instance.LearningOutcomes.OrderBy(lo => lo.SortOrder).Select(lo => new
            {
                lo.Id,
                lo.Code,
                lo.Description,
                lo.BloomLevel,
                lo.SortOrder
            }),
            Exams = instance.Exams.OrderBy(e => e.Date).Select(e => new
            {
                e.Id,
                e.Name,
                e.Type,
                e.Weight,
                e.Date
            })
        });
    }

    // Learning Outcomes
    [HttpGet("{id}/instances/{instanceId}/learning-outcomes")]
    public async Task<IActionResult> GetLearningOutcomes(Guid id, Guid instanceId)
    {
        var instance = await _context.CourseInstances
            .Include(i => i.LearningOutcomes)
                .ThenInclude(lo => lo.ProgramOutcomeMappings)
                    .ThenInclude(m => m.ProgramOutcome)
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.CourseId == id);

        if (instance == null)
            return NotFound();

        var outcomes = instance.LearningOutcomes
            .OrderBy(lo => lo.SortOrder)
            .Select(lo => new
            {
                lo.Id,
                lo.Code,
                lo.Description,
                lo.BloomLevel,
                lo.SortOrder,
                ProgramOutcomeMappings = lo.ProgramOutcomeMappings.Select(m => new
                {
                    m.ProgramOutcomeId,
                    ProgramOutcomeCode = m.ProgramOutcome.Code,
                    m.ContributionLevel
                })
            });

        return Ok(outcomes);
    }

    [HttpPost("{id}/instances/{instanceId}/learning-outcomes")]
    public async Task<IActionResult> AddLearningOutcome(Guid id, Guid instanceId, [FromBody] CreateLearningOutcomeRequest request)
    {
        var instance = await _context.CourseInstances
            .Include(i => i.LearningOutcomes)
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.CourseId == id);

        if (instance == null)
            return NotFound();

        var sortOrder = instance.LearningOutcomes.Count + 1;
        var outcome = new LearningOutcome(instanceId, request.Code, request.Description, request.BloomLevel, sortOrder);
        _context.LearningOutcomes.Add(outcome);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created learning outcome: {Code} for instance {InstanceId}", request.Code, instanceId);
        return CreatedAtAction(nameof(GetLearningOutcomes), new { id, instanceId }, new { outcome.Id });
    }

    [HttpPut("{id}/instances/{instanceId}/learning-outcomes/{loId}")]
    public async Task<IActionResult> UpdateLearningOutcome(Guid id, Guid instanceId, Guid loId, [FromBody] UpdateLearningOutcomeRequest request)
    {
        var outcome = await _context.LearningOutcomes
            .FirstOrDefaultAsync(lo => lo.Id == loId && lo.CourseInstanceId == instanceId);

        if (outcome == null)
            return NotFound();

        outcome.Update(request.Code, request.Description, request.BloomLevel);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}/instances/{instanceId}/learning-outcomes/{loId}")]
    public async Task<IActionResult> DeleteLearningOutcome(Guid id, Guid instanceId, Guid loId)
    {
        var outcome = await _context.LearningOutcomes
            .FirstOrDefaultAsync(lo => lo.Id == loId && lo.CourseInstanceId == instanceId);

        if (outcome == null)
            return NotFound();

        _context.LearningOutcomes.Remove(outcome);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // LO-PO Mappings
    [HttpPost("{id}/instances/{instanceId}/learning-outcomes/{loId}/mappings")]
    public async Task<IActionResult> AddLOPOMapping(Guid id, Guid instanceId, Guid loId, [FromBody] CreateLOPOMappingRequest request)
    {
        var outcome = await _context.LearningOutcomes
            .Include(lo => lo.ProgramOutcomeMappings)
            .FirstOrDefaultAsync(lo => lo.Id == loId && lo.CourseInstanceId == instanceId);

        if (outcome == null)
            return NotFound();

        var programOutcome = await _context.ProgramOutcomes.FindAsync(request.ProgramOutcomeId);
        if (programOutcome == null)
            return BadRequest(new { error = "Program outcome not found" });

        // Check if mapping already exists
        if (outcome.ProgramOutcomeMappings.Any(m => m.ProgramOutcomeId == request.ProgramOutcomeId))
            return BadRequest(new { error = "Mapping already exists" });

        var mapping = new LearningOutcomeProgramOutcomeMapping(loId, request.ProgramOutcomeId, request.ContributionLevel);
        _context.LearningOutcomeProgramOutcomeMappings.Add(mapping);
        await _context.SaveChangesAsync();

        return Created("", new { mapping.Id });
    }

    [HttpPut("{id}/instances/{instanceId}/learning-outcomes/{loId}/mappings/{mappingId}")]
    public async Task<IActionResult> UpdateLOPOMapping(Guid id, Guid instanceId, Guid loId, Guid mappingId, [FromBody] UpdateLOPOMappingRequest request)
    {
        var mapping = await _context.LearningOutcomeProgramOutcomeMappings
            .FirstOrDefaultAsync(m => m.Id == mappingId && m.LearningOutcomeId == loId);

        if (mapping == null)
            return NotFound();

        mapping.UpdateContributionLevel(request.ContributionLevel);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}/instances/{instanceId}/learning-outcomes/{loId}/mappings/{mappingId}")]
    public async Task<IActionResult> DeleteLOPOMapping(Guid id, Guid instanceId, Guid loId, Guid mappingId)
    {
        var mapping = await _context.LearningOutcomeProgramOutcomeMappings
            .FirstOrDefaultAsync(m => m.Id == mappingId && m.LearningOutcomeId == loId);

        if (mapping == null)
            return NotFound();

        _context.LearningOutcomeProgramOutcomeMappings.Remove(mapping);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // LO-PO Matrix (summary view)
    [HttpGet("{id}/instances/{instanceId}/lo-po-matrix")]
    public async Task<IActionResult> GetLOPOMatrix(Guid id, Guid instanceId)
    {
        var instance = await _context.CourseInstances
            .Include(i => i.Course)
                .ThenInclude(c => c.Program)
                    .ThenInclude(p => p.ProgramOutcomes.Where(po => po.IsActive))
            .Include(i => i.LearningOutcomes)
                .ThenInclude(lo => lo.ProgramOutcomeMappings)
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.CourseId == id);

        if (instance == null)
            return NotFound();

        var programOutcomes = instance.Course.Program.ProgramOutcomes
            .OrderBy(po => po.SortOrder)
            .Select(po => new { po.Id, po.Code, po.Description })
            .ToList();

        var learningOutcomes = instance.LearningOutcomes
            .OrderBy(lo => lo.SortOrder)
            .Select(lo => new
            {
                lo.Id,
                lo.Code,
                lo.Description,
                Mappings = programOutcomes.Select(po =>
                {
                    var mapping = lo.ProgramOutcomeMappings.FirstOrDefault(m => m.ProgramOutcomeId == po.Id);
                    return new
                    {
                        ProgramOutcomeId = po.Id,
                        ProgramOutcomeCode = po.Code,
                        ContributionLevel = mapping?.ContributionLevel ?? 0
                    };
                })
            })
            .ToList();

        return Ok(new
        {
            CourseCode = instance.Course.Code,
            CourseName = instance.Course.Name,
            AcademicYear = instance.AcademicYear,
            Semester = instance.Semester,
            ProgramOutcomes = programOutcomes,
            LearningOutcomes = learningOutcomes
        });
    }
}

// Request DTOs
public record CreateCourseRequest(
    Guid ProgramId,
    string Code,
    string Name,
    int Credits,
    int TheoryHours,
    int PracticeHours,
    int Semester,
    bool IsElective = false
);

public record UpdateCourseRequest(
    string Code,
    string Name,
    int Credits,
    int TheoryHours,
    int PracticeHours,
    int Semester
);

public record CreateInstanceRequest(
    string AcademicYear,
    Semester Semester,
    Guid? InstructorId = null
);

public record CreateLearningOutcomeRequest(
    string Code,
    string Description,
    BloomLevel? BloomLevel = null
);

public record UpdateLearningOutcomeRequest(
    string Code,
    string Description,
    BloomLevel? BloomLevel = null
);

public record CreateLOPOMappingRequest(
    Guid ProgramOutcomeId,
    int ContributionLevel
);

public record UpdateLOPOMappingRequest(int ContributionLevel);
