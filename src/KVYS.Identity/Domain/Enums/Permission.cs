namespace KVYS.Identity.Domain.Enums;

/// <summary>
/// System permissions for RBAC.
/// </summary>
public enum Permission
{
    // Course Management
    CourseView = 100,
    CourseCreate = 101,
    CourseEdit = 102,
    CourseDelete = 103,

    // Learning Outcomes
    LearningOutcomeView = 200,
    LearningOutcomeManage = 201,

    // Program Outcomes
    ProgramOutcomeView = 300,
    ProgramOutcomeManage = 301,

    // Exams
    ExamView = 400,
    ExamCreate = 401,
    ExamEdit = 402,
    ExamDelete = 403,
    ExamScoreEntry = 404,

    // Quality Indicators
    IndicatorView = 500,
    IndicatorDataEntry = 501,
    IndicatorApprove = 502,
    IndicatorReject = 503,

    // Surveys
    SurveyView = 600,
    SurveyCreate = 601,
    SurveyManage = 602,
    SurveyRespond = 603,

    // Reports
    ReportView = 700,
    ReportGenerate = 701,
    ReportExport = 702,

    // Archive
    ArchiveView = 800,
    ArchiveUpload = 801,
    ArchiveDownload = 802,
    ArchiveDelete = 803,

    // Administration
    UserView = 900,
    UserManage = 901,
    RoleView = 910,
    RoleManage = 911,
    SystemConfigure = 920,
    AuditLogView = 930
}
