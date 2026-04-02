using KVYS.Education.Infrastructure.Persistence;
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
public class ReportsController : ControllerBase
{
    private readonly EducationDbContext _educationDb;
    private readonly QualityIndicatorsDbContext _qualityDb;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        EducationDbContext educationDb,
        QualityIndicatorsDbContext qualityDb,
        ILogger<ReportsController> logger)
    {
        _educationDb = educationDb;
        _qualityDb = qualityDb;
        _logger = logger;
    }

    // Dashboard summary
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] string? academicYear = null)
    {
        academicYear ??= GetCurrentAcademicYear();

        // Education stats
        var programCount = await _educationDb.Programs.CountAsync(p => p.IsActive);
        var courseCount = await _educationDb.Courses.CountAsync(c => c.IsActive);
        var instanceCount = await _educationDb.CourseInstances
            .CountAsync(i => i.AcademicYear == academicYear);

        // Quality indicator stats
        var indicatorCount = await _qualityDb.Indicators.CountAsync(i => i.IsActive);
        var entryStats = await _qualityDb.IndicatorEntries
            .Where(e => e.AcademicYear == academicYear)
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            AcademicYear = academicYear,
            GeneratedAt = DateTime.UtcNow,
            Education = new
            {
                Programs = programCount,
                Courses = courseCount,
                ActiveInstances = instanceCount
            },
            QualityIndicators = new
            {
                TotalIndicators = indicatorCount,
                Entries = new
                {
                    Draft = entryStats.FirstOrDefault(s => s.Status == EntryStatus.Draft)?.Count ?? 0,
                    Submitted = entryStats.FirstOrDefault(s => s.Status == EntryStatus.Submitted)?.Count ?? 0,
                    Approved = entryStats.FirstOrDefault(s => s.Status == EntryStatus.Approved)?.Count ?? 0,
                    NeedsRevision = entryStats.FirstOrDefault(s => s.Status == EntryStatus.NeedsRevision)?.Count ?? 0
                }
            }
        });
    }

    // Program evaluation report
    [HttpGet("program/{programId}")]
    public async Task<IActionResult> GetProgramReport(Guid programId, [FromQuery] string? academicYear = null)
    {
        academicYear ??= GetCurrentAcademicYear();

        var program = await _educationDb.Programs
            .Include(p => p.Department)
                .ThenInclude(d => d.Faculty)
            .Include(p => p.ProgramOutcomes.Where(po => po.IsActive))
            .Include(p => p.Courses.Where(c => c.IsActive))
                .ThenInclude(c => c.Instances.Where(i => i.AcademicYear == academicYear))
                    .ThenInclude(i => i.LearningOutcomes)
                        .ThenInclude(lo => lo.ProgramOutcomeMappings)
            .FirstOrDefaultAsync(p => p.Id == programId);

        if (program == null)
            return NotFound();

        // Calculate PO coverage
        var poAnalysis = program.ProgramOutcomes.Select(po =>
        {
            var mappings = program.Courses
                .SelectMany(c => c.Instances)
                .SelectMany(i => i.LearningOutcomes)
                .SelectMany(lo => lo.ProgramOutcomeMappings)
                .Where(m => m.ProgramOutcomeId == po.Id)
                .ToList();

            var contributingCourses = program.Courses
                .Where(c => c.Instances
                    .SelectMany(i => i.LearningOutcomes)
                    .Any(lo => lo.ProgramOutcomeMappings.Any(m => m.ProgramOutcomeId == po.Id)))
                .Select(c => new { c.Code, c.Name })
                .ToList();

            return new
            {
                po.Id,
                po.Code,
                po.Description,
                po.Category,
                ContributingCourseCount = contributingCourses.Count,
                ContributingCourses = contributingCourses,
                TotalMappings = mappings.Count,
                AverageContribution = mappings.Any() ? Math.Round(mappings.Average(m => m.ContributionLevel), 2) : 0
            };
        }).OrderBy(po => po.Code);

        return Ok(new
        {
            ProgramId = program.Id,
            ProgramName = program.Name,
            ProgramCode = program.Code,
            Level = program.Level,
            Department = program.Department.Name,
            Faculty = program.Department.Faculty.Name,
            AcademicYear = academicYear,
            GeneratedAt = DateTime.UtcNow,
            Statistics = new
            {
                TotalCourses = program.Courses.Count,
                ActiveInstances = program.Courses.Sum(c => c.Instances.Count),
                TotalProgramOutcomes = program.ProgramOutcomes.Count,
                TotalLearningOutcomes = program.Courses.Sum(c => c.Instances.Sum(i => i.LearningOutcomes.Count))
            },
            ProgramOutcomeAnalysis = poAnalysis
        });
    }

    // Course assessment report
    [HttpGet("course/{courseId}/instance/{instanceId}")]
    public async Task<IActionResult> GetCourseReport(Guid courseId, Guid instanceId)
    {
        var instance = await _educationDb.CourseInstances
            .Include(i => i.Course)
                .ThenInclude(c => c.Program)
                    .ThenInclude(p => p.ProgramOutcomes)
            .Include(i => i.LearningOutcomes)
                .ThenInclude(lo => lo.ProgramOutcomeMappings)
            .Include(i => i.Exams)
                .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.Scores)
            .Include(i => i.Exams)
                .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.LearningOutcomeMappings)
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.CourseId == courseId);

        if (instance == null)
            return NotFound();

        // Calculate LO achievement from exams
        var loAnalysis = instance.LearningOutcomes.Select(lo =>
        {
            var relatedQuestions = instance.Exams
                .SelectMany(e => e.Questions)
                .Where(q => q.LearningOutcomeMappings.Any(m => m.LearningOutcomeId == lo.Id))
                .ToList();

            var maxPoints = relatedQuestions.Sum(q => q.Points);
            var studentScores = relatedQuestions
                .SelectMany(q => q.Scores)
                .GroupBy(s => s.StudentId)
                .Select(g => g.Sum(s => s.Score))
                .ToList();

            var achievementRate = maxPoints > 0 && studentScores.Any()
                ? Math.Round(studentScores.Average() / maxPoints * 100, 2)
                : 0;

            return new
            {
                lo.Id,
                lo.Code,
                lo.Description,
                lo.BloomLevel,
                MaxPoints = maxPoints,
                StudentCount = studentScores.Count,
                AverageScore = studentScores.Any() ? Math.Round(studentScores.Average(), 2) : 0,
                AchievementRate = achievementRate,
                ProgramOutcomeMappings = lo.ProgramOutcomeMappings.Select(m =>
                {
                    var po = instance.Course.Program.ProgramOutcomes.First(p => p.Id == m.ProgramOutcomeId);
                    return new
                    {
                        po.Code,
                        m.ContributionLevel,
                        WeightedContribution = Math.Round(achievementRate * m.ContributionLevel / 3, 2)
                    };
                })
            };
        });

        // Calculate PO achievement (derived from LO achievements)
        var poAchievement = instance.Course.Program.ProgramOutcomes.Select(po =>
        {
            var contributions = loAnalysis
                .SelectMany(lo => lo.ProgramOutcomeMappings)
                .Where(m => m.Code == po.Code)
                .ToList();

            var averageAchievement = contributions.Any()
                ? Math.Round(contributions.Average(c => c.WeightedContribution), 2)
                : 0;

            return new
            {
                po.Code,
                po.Description,
                ContributingLOCount = contributions.Count,
                AverageAchievement = averageAchievement
            };
        }).Where(po => po.ContributingLOCount > 0);

        return Ok(new
        {
            CourseId = instance.Course.Id,
            CourseCode = instance.Course.Code,
            CourseName = instance.Course.Name,
            AcademicYear = instance.AcademicYear,
            Semester = instance.Semester,
            GeneratedAt = DateTime.UtcNow,
            Statistics = new
            {
                LearningOutcomeCount = instance.LearningOutcomes.Count,
                ExamCount = instance.Exams.Count,
                TotalQuestions = instance.Exams.Sum(e => e.Questions.Count),
                UniqueStudents = instance.Exams.SelectMany(e => e.Questions).SelectMany(q => q.Scores).Select(s => s.StudentId).Distinct().Count()
            },
            LearningOutcomeAnalysis = loAnalysis,
            ProgramOutcomeContribution = poAchievement
        });
    }

    // YÖKAK KIDR Report
    [HttpGet("kidr/{academicYear}")]
    public async Task<IActionResult> GetKIDRReport(string academicYear, [FromQuery] Guid? unitId = null)
    {
        // Load categories with children using AsNoTracking for better performance
        var categories = await _qualityDb.IndicatorCategories
            .AsNoTracking()
            .Where(c => c.ParentId == null)
            .Include(c => c.Children)
            .OrderBy(c => c.Code)
            .ToListAsync();

        // Load all active indicators separately with explicit DTO projection
        var indicators = await _qualityDb.Indicators
            .AsNoTracking()
            .Where(i => i.IsActive)
            .Select(i => new
            {
                i.Id,
                i.CategoryId,
                i.Code,
                i.Name,
                i.Unit,
                i.TargetValue,
                i.TargetOperator
            })
            .ToListAsync();

        // Load entries for the academic year
        var entries = await _qualityDb.IndicatorEntries
            .AsNoTracking()
            .Where(e => e.AcademicYear == academicYear && e.Status == EntryStatus.Approved)
            .Where(e => unitId == null || e.UnitId == unitId)
            .Select(e => new
            {
                e.IndicatorId,
                e.NumericValue,
                e.TextValue,
                e.Status,
                TargetValue = e.Indicator.TargetValue,
                TargetOperator = e.Indicator.TargetOperator
            })
            .ToListAsync();

        // Build report using materialized DTOs
        var reportCategories = new List<object>();
        foreach (var cat in categories)
        {
            var subCategoryList = new List<object>();
            var children = cat.Children ?? Enumerable.Empty<IndicatorCategory>();

            foreach (var sub in children.OrderBy(c => c.Code))
            {
                var subIndicatorList = new List<object>();
                var subIndicators = indicators.Where(i => i.CategoryId == sub.Id).OrderBy(i => i.Code);

                foreach (var ind in subIndicators)
                {
                    var entry = entries.FirstOrDefault(e => e.IndicatorId == ind.Id);
                    var targetMet = entry != null && entry.NumericValue.HasValue && ind.TargetValue.HasValue
                        ? EvaluateTarget(entry.NumericValue.Value, ind.TargetValue.Value, ind.TargetOperator)
                        : (bool?)null;

                    subIndicatorList.Add(new
                    {
                        ind.Code,
                        ind.Name,
                        ind.Unit,
                        ind.TargetValue,
                        ind.TargetOperator,
                        ActualValue = entry?.NumericValue,
                        TextValue = entry?.TextValue,
                        TargetMet = targetMet,
                        Status = entry?.Status.ToString() ?? "NotEntered"
                    });
                }

                subCategoryList.Add(new
                {
                    sub.Code,
                    sub.Name,
                    Indicators = subIndicatorList
                });
            }

            var directIndicatorList = new List<object>();
            var catIndicators = indicators.Where(i => i.CategoryId == cat.Id).OrderBy(i => i.Code);

            foreach (var ind in catIndicators)
            {
                var entry = entries.FirstOrDefault(e => e.IndicatorId == ind.Id);
                directIndicatorList.Add(new
                {
                    ind.Code,
                    ind.Name,
                    ind.Unit,
                    ind.TargetValue,
                    ind.TargetOperator,
                    ActualValue = entry?.NumericValue,
                    Status = entry?.Status.ToString() ?? "NotEntered"
                });
            }

            reportCategories.Add(new
            {
                cat.Code,
                cat.Name,
                SubCategories = subCategoryList,
                DirectIndicators = directIndicatorList
            });
        }

        // Summary statistics
        var completedCount = entries.Select(e => e.IndicatorId).Distinct().Count();
        var targetMetCount = entries.Count(e =>
            e.NumericValue.HasValue &&
            e.TargetValue.HasValue &&
            EvaluateTarget(e.NumericValue.Value, e.TargetValue.Value, e.TargetOperator));

        return Ok(new
        {
            ReportType = "YÖKAK KIDR",
            AcademicYear = academicYear,
            UnitId = unitId,
            GeneratedAt = DateTime.UtcNow,
            Summary = new
            {
                TotalIndicators = indicators.Count,
                CompletedIndicators = completedCount,
                CompletionRate = indicators.Count > 0
                    ? Math.Round((decimal)completedCount / indicators.Count * 100, 2)
                    : 0,
                TargetsMet = targetMetCount,
                TargetMetRate = completedCount > 0
                    ? Math.Round((decimal)targetMetCount / completedCount * 100, 2)
                    : 0
            },
            Categories = reportCategories
        });
    }

    private static string GetCurrentAcademicYear()
    {
        var now = DateTime.Now;
        var year = now.Month >= 9 ? now.Year : now.Year - 1;
        return $"{year}-{year + 1}";
    }

    private static bool EvaluateTarget(decimal value, decimal target, string? op)
    {
        return op switch
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
