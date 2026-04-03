using KVYS.Education.Domain.Entities;
using KVYS.Education.Domain.Enums;
using KVYS.Education.Infrastructure.Persistence;
using KVYS.Identity.Domain.Entities;
using KVYS.Identity.Domain.Enums;
using KVYS.Identity.Infrastructure.Persistence;
using KVYS.QualityIndicators.Domain.Entities;
using KVYS.QualityIndicators.Domain.Enums;
using KVYS.QualityIndicators.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcademicProgram = KVYS.Education.Domain.Entities.Program;

namespace KVYS.Api.Infrastructure.Seeding;

public class DatabaseSeeder
{
    private readonly IdentityDbContext _identityDb;
    private readonly EducationDbContext _educationDb;
    private readonly QualityIndicatorsDbContext _qualityDb;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IdentityDbContext identityDb,
        EducationDbContext educationDb,
        QualityIndicatorsDbContext qualityDb,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _identityDb = identityDb;
        _educationDb = educationDb;
        _qualityDb = qualityDb;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAndPermissionsAsync();
        await SeedUsersAsync();
        await SeedEducationDataAsync();
        await SeedQualityIndicatorsAsync();
    }

    private async Task SeedRolesAndPermissionsAsync()
    {
        var roles = new (SystemRole role, string name, string description, Permission[] permissions)[]
        {
            (SystemRole.SystemAdmin, "Sistem Yöneticisi", "Tüm sistem yetkilerine sahip", Enum.GetValues<Permission>()),
            (SystemRole.QualityCoordinator, "Kalite Koordinatörü", "Üniversite geneli kalite takibi",
                new[] {
                    Permission.CourseView, Permission.CourseCreate, Permission.CourseEdit,
                    Permission.LearningOutcomeView, Permission.LearningOutcomeManage,
                    Permission.ProgramOutcomeView, Permission.ProgramOutcomeManage,
                    Permission.IndicatorView, Permission.IndicatorDataEntry, Permission.IndicatorApprove,
                    Permission.ReportView, Permission.ReportGenerate, Permission.ReportExport,
                    Permission.SurveyView, Permission.SurveyCreate, Permission.SurveyManage
                }),
            (SystemRole.DepartmentHead, "Bölüm Başkanı", "Bölüm düzeyinde yönetim",
                new[] {
                    Permission.CourseView, Permission.CourseCreate, Permission.CourseEdit,
                    Permission.LearningOutcomeView, Permission.LearningOutcomeManage,
                    Permission.ProgramOutcomeView, Permission.ProgramOutcomeManage,
                    Permission.IndicatorView, Permission.IndicatorDataEntry,
                    Permission.ReportView, Permission.ReportGenerate,
                    Permission.ExamView, Permission.ExamCreate, Permission.ExamEdit
                }),
            (SystemRole.ProgramCoordinator, "Program Koordinatörü", "Program düzeyinde yönetim",
                new[] {
                    Permission.CourseView, Permission.CourseEdit,
                    Permission.LearningOutcomeView, Permission.LearningOutcomeManage,
                    Permission.ProgramOutcomeView,
                    Permission.IndicatorView, Permission.IndicatorDataEntry,
                    Permission.ReportView
                }),
            (SystemRole.Instructor, "Öğretim Üyesi", "Ders ve sınav yönetimi",
                new[] {
                    Permission.CourseView, Permission.CourseEdit,
                    Permission.LearningOutcomeView,
                    Permission.ExamView, Permission.ExamCreate, Permission.ExamEdit, Permission.ExamScoreEntry,
                    Permission.ReportView
                }),
            (SystemRole.ExternalStakeholder, "Dış Paydaş", "Sınırlı görüntüleme",
                new[] { Permission.ReportView, Permission.SurveyRespond })
        };

        foreach (var (systemRole, name, description, permissions) in roles)
        {
            if (!await _roleManager.RoleExistsAsync(name))
            {
                var role = new ApplicationRole
                {
                    Name = name,
                    Description = description,
                    IsSystemRole = true,
                    SystemRoleType = systemRole
                };

                await _roleManager.CreateAsync(role);
                _logger.LogInformation("Created role: {RoleName}", name);

                // Add permissions
                foreach (var permission in permissions)
                {
                    _identityDb.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        Permission = permission
                    });
                }
                await _identityDb.SaveChangesAsync();
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        // Admin user
        var adminEmail = "admin@kvys.edu.tr";
        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Sistem",
                LastName = "Yöneticisi",
                Title = "Dr.",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, "Sistem Yöneticisi");
                _logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                _logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
            }
        }
        else
        {
            // Reset password for existing admin
            var token = await _userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            var resetResult = await _userManager.ResetPasswordAsync(existingAdmin, token, "Admin123!");
            if (resetResult.Succeeded)
            {
                _logger.LogInformation("Reset password for admin user: {Email}", adminEmail);
            }
        }

        // Test instructor
        var instructorEmail = "ogretim.uyesi@kvys.edu.tr";
        if (await _userManager.FindByEmailAsync(instructorEmail) == null)
        {
            var instructor = new ApplicationUser
            {
                UserName = instructorEmail,
                Email = instructorEmail,
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                Title = "Prof. Dr.",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(instructor, "Test1234!@#$");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(instructor, "Öğretim Üyesi");
                _logger.LogInformation("Created instructor user: {Email}", instructorEmail);
            }
            else
            {
                _logger.LogError("Failed to create instructor user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
            }
        }
    }

    private async Task SeedEducationDataAsync()
    {
        if (await _educationDb.Faculties.AnyAsync())
            return;

        // Faculty
        var faculty = new Faculty("Mühendislik Fakültesi", "MUH");
        _educationDb.Faculties.Add(faculty);
        await _educationDb.SaveChangesAsync();

        // Department
        var department = new Department(faculty.Id, "Bilgisayar Mühendisliği", "BM");
        _educationDb.Departments.Add(department);
        await _educationDb.SaveChangesAsync();

        // Program
        var program = new AcademicProgram(department.Id, "Bilgisayar Mühendisliği Lisans", "BM-L", ProgramLevel.Undergraduate);
        _educationDb.Programs.Add(program);
        await _educationDb.SaveChangesAsync();

        // Program Outcomes (MÜDEK uyumlu)
        var outcomes = new[]
        {
            ("PÇ1", "Matematik, fen bilimleri ve mühendislik disiplinlerine özgü konularda yeterli bilgi birikimi", "Temel"),
            ("PÇ2", "Bu alanlardaki kuramsal ve uygulamalı bilgileri karmaşık mühendislik problemlerinde kullanabilme becerisi", "Temel"),
            ("PÇ3", "Karmaşık mühendislik problemlerini saptama, tanımlama, formüle etme ve çözme becerisi", "Analiz"),
            ("PÇ4", "Karmaşık bir sistemi, süreci, cihazı veya ürünü gerçekçi kısıtlar altında tasarlama becerisi", "Tasarım"),
            ("PÇ5", "Mühendislik uygulamalarında gereken modern teknik ve araçları seçme ve kullanma becerisi", "Uygulama"),
            ("PÇ6", "Deney tasarlama, deney yapma, verileri analiz etme ve sonuçları yorumlama becerisi", "Analiz"),
            ("PÇ7", "Disiplin içi ve çok disiplinli takımlarda etkin biçimde çalışabilme becerisi", "İletişim"),
            ("PÇ8", "Bireysel çalışma becerisi", "Bireysel"),
            ("PÇ9", "Türkçe sözlü ve yazılı etkin iletişim kurma becerisi", "İletişim"),
            ("PÇ10", "En az bir yabancı dilde sözlü ve yazılı iletişim kurma becerisi", "İletişim"),
            ("PÇ11", "Yaşam boyu öğrenmenin gerekliliği bilinci; bilgiye erişebilme ve kendini sürekli yenileme becerisi", "Yaşam Boyu Öğrenme")
        };

        var sortOrder = 1;
        foreach (var (code, description, category) in outcomes)
        {
            var outcome = new ProgramOutcome(program.Id, code, description, category, sortOrder++);
            _educationDb.ProgramOutcomes.Add(outcome);
        }
        await _educationDb.SaveChangesAsync();

        // Sample Course
        var course = new Course(program.Id, "BIL101", "Programlamaya Giriş", 4, 3, 2, 1);
        _educationDb.Courses.Add(course);
        await _educationDb.SaveChangesAsync();

        // Course Instance - create directly
        var instance = new CourseInstance(course.Id, "2024-2025", Semester.Fall);
        _educationDb.CourseInstances.Add(instance);
        await _educationDb.SaveChangesAsync();

        // Learning Outcomes - create directly
        var lo1 = new LearningOutcome(instance.Id, "ÖÇ1", "Temel programlama kavramlarını açıklayabilir", BloomLevel.Understand, 1);
        var lo2 = new LearningOutcome(instance.Id, "ÖÇ2", "Algoritma tasarlayabilir", BloomLevel.Create, 2);
        var lo3 = new LearningOutcome(instance.Id, "ÖÇ3", "Python ile basit programlar yazabilir", BloomLevel.Apply, 3);
        var lo4 = new LearningOutcome(instance.Id, "ÖÇ4", "Hata ayıklama yapabilir", BloomLevel.Analyze, 4);
        _educationDb.LearningOutcomes.Add(lo1);
        _educationDb.LearningOutcomes.Add(lo2);
        _educationDb.LearningOutcomes.Add(lo3);
        _educationDb.LearningOutcomes.Add(lo4);
        await _educationDb.SaveChangesAsync();

        // Map LOs to POs - create mappings directly
        var programOutcomes = await _educationDb.ProgramOutcomes.Where(po => po.ProgramId == program.Id).ToListAsync();
        var mappings = new[]
        {
            new LearningOutcomeProgramOutcomeMapping(lo1.Id, programOutcomes.First(po => po.Code == "PÇ1").Id, 3),
            new LearningOutcomeProgramOutcomeMapping(lo1.Id, programOutcomes.First(po => po.Code == "PÇ2").Id, 2),
            new LearningOutcomeProgramOutcomeMapping(lo2.Id, programOutcomes.First(po => po.Code == "PÇ3").Id, 3),
            new LearningOutcomeProgramOutcomeMapping(lo2.Id, programOutcomes.First(po => po.Code == "PÇ4").Id, 2),
            new LearningOutcomeProgramOutcomeMapping(lo3.Id, programOutcomes.First(po => po.Code == "PÇ2").Id, 3),
            new LearningOutcomeProgramOutcomeMapping(lo3.Id, programOutcomes.First(po => po.Code == "PÇ5").Id, 3),
            new LearningOutcomeProgramOutcomeMapping(lo4.Id, programOutcomes.First(po => po.Code == "PÇ3").Id, 2),
            new LearningOutcomeProgramOutcomeMapping(lo4.Id, programOutcomes.First(po => po.Code == "PÇ6").Id, 2)
        };
        _educationDb.LearningOutcomeProgramOutcomeMappings.AddRange(mappings);
        await _educationDb.SaveChangesAsync();

        _logger.LogInformation("Seeded education data: 1 faculty, 1 department, 1 program, 11 POs, 1 course, 4 LOs");
    }

    private async Task SeedQualityIndicatorsAsync()
    {
        if (await _qualityDb.IndicatorCategories.AnyAsync())
            return;

        // YÖKAK KIDR Categories
        var categories = new (string code, string name, string? parentCode, (string code, string name, IndicatorDataType type, string? unit, decimal? target, string? op)[] indicators)[]
        {
            ("A", "Liderlik, Yönetim ve Kalite", null, Array.Empty<(string, string, IndicatorDataType, string?, decimal?, string?)>()),
            ("A.1", "Liderlik ve Kalite", "A", new (string, string, IndicatorDataType, string?, decimal?, string?)[]
            {
                ("A.1.1", "Kalite güvence sistemi kapsamı", IndicatorDataType.Percentage, "%", 100m, ">="),
                ("A.1.2", "Kalite komisyonu toplantı sayısı", IndicatorDataType.Number, "adet", 4m, ">=")
            }),
            ("A.2", "Paydaş Katılımı", "A", new (string, string, IndicatorDataType, string?, decimal?, string?)[]
            {
                ("A.2.1", "Dış paydaş toplantı sayısı", IndicatorDataType.Number, "adet", 2m, ">="),
                ("A.2.2", "Mezun anketi katılım oranı", IndicatorDataType.Percentage, "%", 30m, ">=")
            }),

            ("B", "Eğitim ve Öğretim", null, Array.Empty<(string, string, IndicatorDataType, string?, decimal?, string?)>()),
            ("B.1", "Program Tasarımı", "B", new (string, string, IndicatorDataType, string?, decimal?, string?)[]
            {
                ("B.1.1", "Akredite program oranı", IndicatorDataType.Percentage, "%", 50m, ">="),
                ("B.1.2", "Program güncelleme oranı", IndicatorDataType.Percentage, "%", 100m, "=")
            }),
            ("B.2", "Öğrenme Kaynakları", "B", new (string, string, IndicatorDataType, string?, decimal?, string?)[]
            {
                ("B.2.1", "Öğrenci başına kitap sayısı", IndicatorDataType.Number, "adet", 10m, ">="),
                ("B.2.2", "E-kaynak erişim oranı", IndicatorDataType.Percentage, "%", 90m, ">=")
            }),
            ("B.3", "Öğrenme-Öğretme Süreci", "B", new (string, string, IndicatorDataType, string?, decimal?, string?)[]
            {
                ("B.3.1", "Öğrenci/öğretim üyesi oranı", IndicatorDataType.Ratio, null, 25m, "<="),
                ("B.3.2", "Ders değerlendirme anketi katılım oranı", IndicatorDataType.Percentage, "%", 70m, ">=")
            }),
            ("B.4", "Ölçme ve Değerlendirme", "B", new (string, string, IndicatorDataType, string?, decimal?, string?)[]
            {
                ("B.4.1", "Mezuniyet oranı", IndicatorDataType.Percentage, "%", 70m, ">="),
                ("B.4.2", "Mezun istihdam oranı (6 ay)", IndicatorDataType.Percentage, "%", 70m, ">=")
            }),

            ("C", "Araştırma ve Geliştirme", null, Array.Empty<(string, string, IndicatorDataType, string?, decimal?, string?)>()),
            ("C.1", "Araştırma Kaynakları", "C", new (string, string, IndicatorDataType, string?, decimal?, string?)[]
            {
                ("C.1.1", "Öğretim üyesi başına araştırma bütçesi", IndicatorDataType.Number, "TL", 50000m, ">=")
            }),
            ("C.2", "Araştırma Performansı", "C", new (string, string, IndicatorDataType, string?, decimal?, string?)[]
            {
                ("C.2.1", "Öğretim üyesi başına yayın sayısı", IndicatorDataType.Number, "adet", 1.5m, ">="),
                ("C.2.2", "Öğretim üyesi başına atıf sayısı", IndicatorDataType.Number, "adet", 10m, ">="),
                ("C.2.3", "Öğretim üyesi başına proje sayısı", IndicatorDataType.Number, "adet", 0.5m, ">=")
            }),

            ("D", "Toplumsal Katkı", null, Array.Empty<(string, string, IndicatorDataType, string?, decimal?, string?)>()),
            ("D.1", "Toplumla Etkileşim", "D", new (string, string, IndicatorDataType, string?, decimal?, string?)[]
            {
                ("D.1.1", "Toplumsal katkı projesi sayısı", IndicatorDataType.Number, "adet", 5m, ">="),
                ("D.1.2", "Sanayi işbirliği projesi sayısı", IndicatorDataType.Number, "adet", 3m, ">=")
            })
        };

        var categoryDict = new Dictionary<string, IndicatorCategory>();

        foreach (var (code, name, parentCode, indicators) in categories)
        {
            Guid? parentId = null;
            if (parentCode != null && categoryDict.TryGetValue(parentCode, out var parent))
            {
                parentId = parent.Id;
            }

            var category = new IndicatorCategory(code, name, null, parentId);
            _qualityDb.IndicatorCategories.Add(category);
            await _qualityDb.SaveChangesAsync();

            categoryDict[code] = category;

            foreach (var (iCode, iName, dataType, unit, target, op) in indicators)
            {
                var indicator = new Indicator(
                    category.Id,
                    iCode,
                    iName,
                    dataType,
                    CollectionFrequency.Annual,
                    null,
                    unit
                );

                if (target.HasValue && !string.IsNullOrEmpty(op))
                {
                    indicator.SetTarget(target.Value, op);
                }

                _qualityDb.Indicators.Add(indicator);
            }
            await _qualityDb.SaveChangesAsync();
        }

        _logger.LogInformation("Seeded quality indicators: {CategoryCount} categories, {IndicatorCount} indicators",
            categoryDict.Count,
            await _qualityDb.Indicators.CountAsync());
    }
}
