using KVYS.Education.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KVYS.Education.Infrastructure.Persistence;

/// <summary>
/// Database context for education module.
/// </summary>
public class EducationDbContext : DbContext
{
    public EducationDbContext(DbContextOptions<EducationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Program> Programs => Set<Program>();
    public DbSet<ProgramOutcome> ProgramOutcomes => Set<ProgramOutcome>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseInstance> CourseInstances => Set<CourseInstance>();
    public DbSet<LearningOutcome> LearningOutcomes => Set<LearningOutcome>();
    public DbSet<LearningOutcomeProgramOutcomeMapping> LearningOutcomeProgramOutcomeMappings => Set<LearningOutcomeProgramOutcomeMapping>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<QuestionLearningOutcomeMapping> QuestionLearningOutcomeMappings => Set<QuestionLearningOutcomeMapping>();
    public DbSet<StudentScore> StudentScores => Set<StudentScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("education");

        // Faculty
        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.ToTable("Faculties");
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).HasMaxLength(200).IsRequired();
            entity.Property(f => f.Code).HasMaxLength(20).IsRequired();
            entity.HasIndex(f => f.Code).IsUnique();
        });

        // Department
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).HasMaxLength(200).IsRequired();
            entity.Property(d => d.Code).HasMaxLength(20).IsRequired();
            entity.HasIndex(d => d.Code).IsUnique();
            entity.HasOne(d => d.Faculty)
                .WithMany(f => f.Departments)
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Program
        modelBuilder.Entity<Program>(entity =>
        {
            entity.ToTable("Programs");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Code).HasMaxLength(20).IsRequired();
            entity.Property(p => p.AccreditationStatus).HasMaxLength(50);
            entity.HasOne(p => p.Department)
                .WithMany(d => d.Programs)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProgramOutcome
        modelBuilder.Entity<ProgramOutcome>(entity =>
        {
            entity.ToTable("ProgramOutcomes");
            entity.HasKey(po => po.Id);
            entity.Property(po => po.Code).HasMaxLength(10).IsRequired();
            entity.Property(po => po.Description).IsRequired();
            entity.Property(po => po.Category).HasMaxLength(50);
            entity.HasOne(po => po.Program)
                .WithMany(p => p.ProgramOutcomes)
                .HasForeignKey(po => po.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Course
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Courses");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Code).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(c => c.Program)
                .WithMany(p => p.Courses)
                .HasForeignKey(c => c.ProgramId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CourseInstance
        modelBuilder.Entity<CourseInstance>(entity =>
        {
            entity.ToTable("CourseInstances");
            entity.HasKey(ci => ci.Id);
            entity.Property(ci => ci.AcademicYear).HasMaxLength(10).IsRequired();
            entity.Property(ci => ci.Status).HasMaxLength(20);
            entity.HasIndex(ci => new { ci.CourseId, ci.AcademicYear, ci.Semester }).IsUnique();
            entity.HasOne(ci => ci.Course)
                .WithMany(c => c.Instances)
                .HasForeignKey(ci => ci.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // LearningOutcome
        modelBuilder.Entity<LearningOutcome>(entity =>
        {
            entity.ToTable("LearningOutcomes");
            entity.HasKey(lo => lo.Id);
            entity.Property(lo => lo.Code).HasMaxLength(10).IsRequired();
            entity.Property(lo => lo.Description).IsRequired();
            entity.HasOne(lo => lo.CourseInstance)
                .WithMany(ci => ci.LearningOutcomes)
                .HasForeignKey(lo => lo.CourseInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LearningOutcomeProgramOutcomeMapping
        modelBuilder.Entity<LearningOutcomeProgramOutcomeMapping>(entity =>
        {
            entity.ToTable("LearningOutcomeProgramOutcomeMappings");
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.LearningOutcomeId, m.ProgramOutcomeId }).IsUnique();
            entity.HasOne(m => m.LearningOutcome)
                .WithMany(lo => lo.ProgramOutcomeMappings)
                .HasForeignKey(m => m.LearningOutcomeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.ProgramOutcome)
                .WithMany(po => po.LearningOutcomeMappings)
                .HasForeignKey(m => m.ProgramOutcomeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Exam
        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("Exams");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Weight).HasPrecision(5, 2);
            entity.Property(e => e.TotalPoints).HasPrecision(6, 2);
            entity.HasOne(e => e.CourseInstance)
                .WithMany(ci => ci.Exams)
                .HasForeignKey(e => e.CourseInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExamQuestion
        modelBuilder.Entity<ExamQuestion>(entity =>
        {
            entity.ToTable("ExamQuestions");
            entity.HasKey(eq => eq.Id);
            entity.Property(eq => eq.Points).HasPrecision(5, 2);
            entity.HasOne(eq => eq.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(eq => eq.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuestionLearningOutcomeMapping
        modelBuilder.Entity<QuestionLearningOutcomeMapping>(entity =>
        {
            entity.ToTable("QuestionLearningOutcomeMappings");
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.QuestionId, m.LearningOutcomeId }).IsUnique();
            entity.HasOne(m => m.Question)
                .WithMany(q => q.LearningOutcomeMappings)
                .HasForeignKey(m => m.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.LearningOutcome)
                .WithMany(lo => lo.QuestionMappings)
                .HasForeignKey(m => m.LearningOutcomeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StudentScore
        modelBuilder.Entity<StudentScore>(entity =>
        {
            entity.ToTable("StudentScores");
            entity.HasKey(ss => ss.Id);
            entity.Property(ss => ss.StudentId).HasMaxLength(20).IsRequired();
            entity.Property(ss => ss.Score).HasPrecision(5, 2);
            entity.HasIndex(ss => new { ss.QuestionId, ss.StudentId }).IsUnique();
            entity.HasOne(ss => ss.Question)
                .WithMany(q => q.Scores)
                .HasForeignKey(ss => ss.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
