namespace KVYS.Web.Models;

public record LearningOutcomeDto(
    Guid Id,
    string Code,
    string Description,
    string BloomLevel,
    List<string>? MappedPOs);

public record ProgramOutcomeDto(
    Guid Id,
    string Code,
    string Description,
    string Category);

public record ExamDto(
    Guid Id,
    string Name,
    string Type,
    DateTime? Date,
    int QuestionCount,
    int TotalPoints,
    decimal Weight);

public record CourseInstanceDto(
    Guid Id,
    string CourseCode,
    string CourseName,
    string AcademicYear,
    string Semester,
    string? InstructorName,
    Guid ProgramId);

public record CourseDto(
    Guid Id,
    string Code,
    string Name,
    int Credits,
    int Ects,
    string Type,
    int Semester,
    bool IsActive,
    Guid ProgramId,
    string ProgramCode);

public record ProgramDto(
    Guid Id,
    string Code,
    string Name);

public record ProgramDetailDto(
    Guid Id,
    string Code,
    string Name,
    string Level,
    bool IsActive,
    Guid DepartmentId,
    string DepartmentName,
    Guid FacultyId,
    int OutcomeCount);

public record FacultyDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    int DepartmentCount);

public record DepartmentDto(
    Guid Id,
    string Code,
    string Name);

public record FacultyDetailDto(
    Guid Id,
    string Name,
    IEnumerable<DepartmentDto>? Departments);

// Indicator DTOs
public record IndicatorCategoryDto(
    Guid Id,
    string Code,
    string Name,
    Guid? ParentId,
    int IndicatorCount,
    List<IndicatorCategoryDto>? Children);

public record IndicatorDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string DataType,
    string? Unit,
    decimal? TargetValue,
    string Direction,
    Guid CategoryId,
    string CategoryName,
    bool IsActive);

public record IndicatorEntryDto(
    Guid Id,
    Guid IndicatorId,
    string IndicatorCode,
    string IndicatorName,
    string AcademicYear,
    Guid UnitId,
    string UnitName,
    decimal? NumericValue,
    string? TextValue,
    string Status,
    DateTime CreatedAt,
    string? CreatedByName);

public record UnitDto(
    Guid Id,
    string Code,
    string Name,
    string Type);

// Report DTOs
public record KIDRReportDto(
    string AcademicYear,
    List<KIDRCategoryDto> Categories,
    KIDRSummaryDto Summary);

public record KIDRCategoryDto(
    Guid CategoryId,
    string CategoryCode,
    string CategoryName,
    List<KIDRIndicatorDto> Indicators,
    int TotalIndicators,
    int CompletedIndicators,
    int TargetMetCount);

public record KIDRIndicatorDto(
    Guid IndicatorId,
    string Code,
    string Name,
    string DataType,
    decimal? TargetValue,
    decimal? ActualValue,
    string? TextValue,
    bool IsTargetMet,
    string Status);

public record KIDRSummaryDto(
    int TotalIndicators,
    int CompletedIndicators,
    int TargetMetCount,
    decimal CompletionRate,
    decimal TargetMetRate);

public record ProgramReportDto(
    Guid ProgramId,
    string ProgramCode,
    string ProgramName,
    string AcademicYear,
    List<POCoverageDto> OutcomeCoverage,
    List<ContributingCourseDto> ContributingCourses);

public record POCoverageDto(
    Guid OutcomeId,
    string Code,
    string Description,
    int ContributingCourseCount,
    decimal CoveragePercentage);

public record ContributingCourseDto(
    Guid CourseId,
    string CourseCode,
    string CourseName,
    List<string> ContributedPOs,
    int LoCount);

public record CourseReportDto(
    Guid CourseId,
    Guid InstanceId,
    string CourseCode,
    string CourseName,
    string AcademicYear,
    string Semester,
    List<LOAchievementDto> LearningOutcomes,
    List<POContributionDto> ProgramOutcomes,
    CourseStatisticsDto Statistics);

public record LOAchievementDto(
    Guid LoId,
    string Code,
    string Description,
    string BloomLevel,
    decimal AchievementRate,
    int StudentCount);

public record POContributionDto(
    Guid PoId,
    string Code,
    string Description,
    decimal ContributionScore,
    List<string> ContributingLOs);

public record CourseStatisticsDto(
    int StudentCount,
    decimal AverageScore,
    decimal PassRate,
    int ExamCount);
