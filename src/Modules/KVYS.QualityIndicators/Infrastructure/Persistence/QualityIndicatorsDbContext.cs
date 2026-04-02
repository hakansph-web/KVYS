using KVYS.QualityIndicators.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KVYS.QualityIndicators.Infrastructure.Persistence;

/// <summary>
/// Database context for quality indicators module.
/// </summary>
public class QualityIndicatorsDbContext : DbContext
{
    public QualityIndicatorsDbContext(DbContextOptions<QualityIndicatorsDbContext> options)
        : base(options)
    {
    }

    public DbSet<IndicatorCategory> IndicatorCategories => Set<IndicatorCategory>();
    public DbSet<Indicator> Indicators => Set<Indicator>();
    public DbSet<IndicatorEntry> IndicatorEntries => Set<IndicatorEntry>();
    public DbSet<EvidenceDocument> EvidenceDocuments => Set<EvidenceDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("quality");

        // IndicatorCategory
        modelBuilder.Entity<IndicatorCategory>(entity =>
        {
            entity.ToTable("IndicatorCategories");
            entity.HasKey(ic => ic.Id);
            entity.Property(ic => ic.Code).HasMaxLength(20).IsRequired();
            entity.Property(ic => ic.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(ic => ic.Code).IsUnique();
            entity.HasOne(ic => ic.Parent)
                .WithMany(ic => ic.Children)
                .HasForeignKey(ic => ic.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Indicator
        modelBuilder.Entity<Indicator>(entity =>
        {
            entity.ToTable("Indicators");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Code).HasMaxLength(20).IsRequired();
            entity.Property(i => i.Name).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Unit).HasMaxLength(50);
            entity.Property(i => i.TargetOperator).HasMaxLength(10);
            entity.Property(i => i.TargetValue).HasPrecision(10, 2);
            entity.HasIndex(i => i.Code).IsUnique();
            entity.HasOne(i => i.Category)
                .WithMany(ic => ic.Indicators)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // IndicatorEntry
        modelBuilder.Entity<IndicatorEntry>(entity =>
        {
            entity.ToTable("IndicatorEntries");
            entity.HasKey(ie => ie.Id);
            entity.Property(ie => ie.UnitType).HasMaxLength(20).IsRequired();
            entity.Property(ie => ie.AcademicYear).HasMaxLength(10).IsRequired();
            entity.Property(ie => ie.Semester).HasMaxLength(10);
            entity.Property(ie => ie.NumericValue).HasPrecision(15, 4);
            entity.HasIndex(ie => new { ie.IndicatorId, ie.UnitId, ie.AcademicYear, ie.Semester }).IsUnique();
            entity.HasOne(ie => ie.Indicator)
                .WithMany(i => i.Entries)
                .HasForeignKey(ie => ie.IndicatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EvidenceDocument
        modelBuilder.Entity<EvidenceDocument>(entity =>
        {
            entity.ToTable("EvidenceDocuments");
            entity.HasKey(ed => ed.Id);
            entity.Property(ed => ed.FileName).HasMaxLength(255).IsRequired();
            entity.Property(ed => ed.ContentType).HasMaxLength(100);
            entity.Property(ed => ed.StoragePath).HasMaxLength(500).IsRequired();
            entity.HasOne(ed => ed.IndicatorEntry)
                .WithMany(ie => ie.EvidenceDocuments)
                .HasForeignKey(ed => ed.IndicatorEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
