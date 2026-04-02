using KVYS.QualityIndicators.Domain.Entities;
using KVYS.QualityIndicators.Domain.Enums;
using KVYS.QualityIndicators.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KVYS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class IndicatorsController : ControllerBase
{
    private readonly QualityIndicatorsDbContext _context;

    public IndicatorsController(QualityIndicatorsDbContext context)
    {
        _context = context;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.IndicatorCategories
            .Where(c => c.ParentId == null)
            .Include(c => c.Children)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.Description,
                Children = c.Children.OrderBy(ch => ch.SortOrder).Select(ch => new
                {
                    ch.Id,
                    ch.Code,
                    ch.Name,
                    ch.Description
                })
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? categoryId = null, [FromQuery] bool? isActive = true)
    {
        var query = _context.Indicators
            .Include(i => i.Category)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(i => i.CategoryId == categoryId.Value);

        if (isActive.HasValue)
            query = query.Where(i => i.IsActive == isActive.Value);

        var indicators = await query
            .Select(i => new
            {
                i.Id,
                i.Code,
                i.Name,
                i.Description,
                i.DataType,
                i.Unit,
                i.TargetValue,
                i.TargetOperator,
                i.CollectionFrequency,
                i.IsRequired,
                i.IsActive,
                Category = new { i.Category.Id, i.Category.Code, i.Category.Name }
            })
            .ToListAsync();

        return Ok(indicators);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var indicator = await _context.Indicators
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (indicator == null)
            return NotFound();

        return Ok(new
        {
            indicator.Id,
            indicator.Code,
            indicator.Name,
            indicator.Description,
            indicator.DataType,
            indicator.Unit,
            indicator.Formula,
            indicator.TargetValue,
            indicator.TargetOperator,
            indicator.CollectionFrequency,
            indicator.IsRequired,
            indicator.IsActive,
            Category = new { indicator.Category.Id, indicator.Category.Code, indicator.Category.Name }
        });
    }

    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
        [FromQuery] Guid? indicatorId = null,
        [FromQuery] Guid? unitId = null,
        [FromQuery] string? academicYear = null,
        [FromQuery] EntryStatus? status = null)
    {
        var query = _context.IndicatorEntries
            .Include(e => e.Indicator)
            .AsQueryable();

        if (indicatorId.HasValue)
            query = query.Where(e => e.IndicatorId == indicatorId.Value);

        if (unitId.HasValue)
            query = query.Where(e => e.UnitId == unitId.Value);

        if (!string.IsNullOrEmpty(academicYear))
            query = query.Where(e => e.AcademicYear == academicYear);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var entries = await query
            .Select(e => new
            {
                e.Id,
                e.IndicatorId,
                IndicatorCode = e.Indicator.Code,
                IndicatorName = e.Indicator.Name,
                e.UnitId,
                e.UnitType,
                e.AcademicYear,
                e.Semester,
                e.NumericValue,
                e.TextValue,
                e.Status,
                e.Notes,
                e.SubmittedAt,
                e.ApprovedAt
            })
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost("entries")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateIndicatorEntryRequest request)
    {
        var indicator = await _context.Indicators.FindAsync(request.IndicatorId);
        if (indicator == null)
            return BadRequest(new { error = "Indicator not found" });

        // Check for existing entry using AsNoTracking and AnyAsync
        var exists = await _context.IndicatorEntries
            .AsNoTracking()
            .AnyAsync(e =>
                e.IndicatorId == request.IndicatorId &&
                e.UnitId == request.UnitId &&
                e.AcademicYear == request.AcademicYear &&
                e.Semester == request.Semester);

        if (exists)
            return BadRequest(new { error = "Entry already exists for this period" });

        var entry = new IndicatorEntry(
            request.IndicatorId,
            request.UnitId,
            request.UnitType,
            request.AcademicYear,
            request.Semester
        );

        if (request.NumericValue.HasValue)
            entry.SetNumericValue(request.NumericValue.Value, request.Notes);
        else if (!string.IsNullOrEmpty(request.TextValue))
            entry.SetTextValue(request.TextValue, request.Notes);

        _context.IndicatorEntries.Add(entry);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEntries), new { indicatorId = entry.IndicatorId }, new { entry.Id });
    }

    [HttpPatch("entries/{id}/submit")]
    public async Task<IActionResult> SubmitEntry(Guid id)
    {
        var entry = await _context.IndicatorEntries.FindAsync(id);
        if (entry == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            entry.Submit(Guid.Parse(userId));
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("entries/{id}/approve")]
    public async Task<IActionResult> ApproveEntry(Guid id)
    {
        var entry = await _context.IndicatorEntries.FindAsync(id);
        if (entry == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            entry.Approve(Guid.Parse(userId));
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("entries/{id}/reject")]
    public async Task<IActionResult> RejectEntry(Guid id, [FromBody] RejectEntryRequest request)
    {
        var entry = await _context.IndicatorEntries.FindAsync(id);
        if (entry == null)
            return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            entry.Reject(Guid.Parse(userId), request.Reason);
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("entries/{id}")]
    public async Task<IActionResult> UpdateEntry(Guid id, [FromBody] UpdateIndicatorEntryRequest request)
    {
        var entry = await _context.IndicatorEntries.FindAsync(id);
        if (entry == null)
            return NotFound();

        if (entry.Status != EntryStatus.Draft && entry.Status != EntryStatus.NeedsRevision)
            return BadRequest(new { error = "Can only update entries in Draft or NeedsRevision status" });

        if (request.NumericValue.HasValue)
            entry.SetNumericValue(request.NumericValue.Value, request.Notes);
        else if (!string.IsNullOrEmpty(request.TextValue))
            entry.SetTextValue(request.TextValue, request.Notes);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("entries/{id}")]
    public async Task<IActionResult> GetEntry(Guid id)
    {
        var entry = await _context.IndicatorEntries
            .Include(e => e.Indicator)
            .Include(e => e.EvidenceDocuments)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null)
            return NotFound();

        return Ok(new
        {
            entry.Id,
            entry.IndicatorId,
            IndicatorCode = entry.Indicator.Code,
            IndicatorName = entry.Indicator.Name,
            entry.UnitId,
            entry.UnitType,
            entry.AcademicYear,
            entry.Semester,
            entry.NumericValue,
            entry.TextValue,
            entry.Status,
            entry.Notes,
            entry.SubmittedBy,
            entry.SubmittedAt,
            entry.ApprovedBy,
            entry.ApprovedAt,
            entry.RejectionReason,
            EvidenceDocuments = entry.EvidenceDocuments.Select(d => new
            {
                d.Id,
                d.FileName,
                d.ContentType,
                d.FileSize,
                d.UploadedAt
            })
        });
    }

    // Reporting endpoints
    [HttpGet("report/summary")]
    public async Task<IActionResult> GetSummaryReport(
        [FromQuery] string academicYear,
        [FromQuery] Guid? unitId = null)
    {
        var query = _context.IndicatorEntries
            .Include(e => e.Indicator)
                .ThenInclude(i => i.Category)
            .Where(e => e.AcademicYear == academicYear && e.Status == EntryStatus.Approved);

        if (unitId.HasValue)
            query = query.Where(e => e.UnitId == unitId.Value);

        var entries = await query.ToListAsync();

        var summary = entries
            .GroupBy(e => new { e.Indicator.Category.Code, e.Indicator.Category.Name })
            .Select(g => new
            {
                CategoryCode = g.Key.Code,
                CategoryName = g.Key.Name,
                TotalIndicators = g.Select(e => e.IndicatorId).Distinct().Count(),
                CompletedCount = g.Count(),
                TargetMetCount = g.Count(e => EvaluateTarget(e)),
                Indicators = g.Select(e => new
                {
                    e.Indicator.Code,
                    e.Indicator.Name,
                    e.NumericValue,
                    e.Indicator.TargetValue,
                    e.Indicator.TargetOperator,
                    e.Indicator.Unit,
                    TargetMet = EvaluateTarget(e)
                })
            })
            .OrderBy(s => s.CategoryCode);

        return Ok(new
        {
            AcademicYear = academicYear,
            UnitId = unitId,
            GeneratedAt = DateTime.UtcNow,
            Categories = summary
        });
    }

    [HttpGet("report/completion")]
    public async Task<IActionResult> GetCompletionReport(
        [FromQuery] string academicYear,
        [FromQuery] Guid? categoryId = null)
    {
        var indicators = await _context.Indicators
            .Where(i => i.IsActive && i.IsRequired)
            .Where(i => categoryId == null || i.CategoryId == categoryId)
            .ToListAsync();

        var entries = await _context.IndicatorEntries
            .Where(e => e.AcademicYear == academicYear)
            .ToListAsync();

        var unitIds = entries.Select(e => e.UnitId).Distinct().ToList();

        var report = unitIds.Select(unitId => new
        {
            UnitId = unitId,
            TotalRequired = indicators.Count,
            Submitted = entries.Count(e => e.UnitId == unitId && e.Status != EntryStatus.Draft),
            Approved = entries.Count(e => e.UnitId == unitId && e.Status == EntryStatus.Approved),
            Pending = entries.Count(e => e.UnitId == unitId && e.Status == EntryStatus.Submitted),
            NeedsRevision = entries.Count(e => e.UnitId == unitId && e.Status == EntryStatus.NeedsRevision),
            Draft = entries.Count(e => e.UnitId == unitId && e.Status == EntryStatus.Draft),
            CompletionRate = indicators.Count > 0
                ? Math.Round((decimal)entries.Count(e => e.UnitId == unitId && e.Status == EntryStatus.Approved) / indicators.Count * 100, 2)
                : 0
        });

        return Ok(new
        {
            AcademicYear = academicYear,
            CategoryId = categoryId,
            GeneratedAt = DateTime.UtcNow,
            TotalIndicators = indicators.Count,
            Units = report.OrderByDescending(r => r.CompletionRate)
        });
    }

    [HttpGet("report/trends")]
    public async Task<IActionResult> GetTrendReport(
        [FromQuery] Guid indicatorId,
        [FromQuery] Guid? unitId = null,
        [FromQuery] int years = 5)
    {
        var indicator = await _context.Indicators.FindAsync(indicatorId);
        if (indicator == null)
            return NotFound();

        var query = _context.IndicatorEntries
            .Where(e => e.IndicatorId == indicatorId && e.Status == EntryStatus.Approved)
            .Where(e => e.NumericValue.HasValue);

        if (unitId.HasValue)
            query = query.Where(e => e.UnitId == unitId.Value);

        var entries = await query
            .OrderByDescending(e => e.AcademicYear)
            .Take(years * 2) // For semesters
            .ToListAsync();

        var trends = entries
            .GroupBy(e => e.AcademicYear)
            .Select(g => new
            {
                AcademicYear = g.Key,
                Values = g.Select(e => new
                {
                    e.Semester,
                    e.NumericValue,
                    e.UnitId
                }),
                Average = g.Average(e => e.NumericValue),
                Min = g.Min(e => e.NumericValue),
                Max = g.Max(e => e.NumericValue)
            })
            .OrderBy(t => t.AcademicYear);

        return Ok(new
        {
            IndicatorId = indicatorId,
            IndicatorCode = indicator.Code,
            IndicatorName = indicator.Name,
            TargetValue = indicator.TargetValue,
            TargetOperator = indicator.TargetOperator,
            Unit = indicator.Unit,
            Trends = trends
        });
    }

    private static bool EvaluateTarget(IndicatorEntry entry)
    {
        if (!entry.NumericValue.HasValue || !entry.Indicator.TargetValue.HasValue)
            return false;

        var value = entry.NumericValue.Value;
        var target = entry.Indicator.TargetValue.Value;

        return entry.Indicator.TargetOperator switch
        {
            ">=" => value >= target,
            "<=" => value <= target,
            ">" => value > target,
            "<" => value < target,
            "=" => value == target,
            _ => false
        };
    }
}

public record CreateIndicatorEntryRequest(
    Guid IndicatorId,
    Guid UnitId,
    string UnitType,
    string AcademicYear,
    string? Semester,
    decimal? NumericValue,
    string? TextValue,
    string? Notes
);

public record UpdateIndicatorEntryRequest(
    decimal? NumericValue,
    string? TextValue,
    string? Notes
);

public record RejectEntryRequest(string Reason);
