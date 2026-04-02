using KVYS.QualityIndicators.Domain.Enums;
using KVYS.Shared.Domain.Primitives;

namespace KVYS.QualityIndicators.Domain.Entities;

/// <summary>
/// Data entry for an indicator by a unit (department/program).
/// </summary>
public class IndicatorEntry : AggregateRoot
{
    private readonly List<EvidenceDocument> _evidenceDocuments = [];

    private IndicatorEntry() { }

    public IndicatorEntry(
        Guid indicatorId,
        Guid unitId,
        string unitType,
        string academicYear,
        string? semester = null)
        : base(Guid.NewGuid())
    {
        IndicatorId = indicatorId;
        UnitId = unitId;
        UnitType = unitType;
        AcademicYear = academicYear;
        Semester = semester;
    }

    public Guid IndicatorId { get; private set; }
    public Guid UnitId { get; private set; }
    public string UnitType { get; private set; } = string.Empty;
    public string AcademicYear { get; private set; } = string.Empty;
    public string? Semester { get; private set; }
    public decimal? NumericValue { get; private set; }
    public string? TextValue { get; private set; }
    public EntryStatus Status { get; private set; } = EntryStatus.Draft;
    public string? Notes { get; private set; }
    public Guid? SubmittedBy { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public virtual Indicator Indicator { get; private set; } = null!;
    public IReadOnlyCollection<EvidenceDocument> EvidenceDocuments => _evidenceDocuments.AsReadOnly();

    public void SetNumericValue(decimal value, string? notes = null)
    {
        NumericValue = value;
        Notes = notes;
        SetUpdatedAt();
    }

    public void SetTextValue(string value, string? notes = null)
    {
        TextValue = value;
        Notes = notes;
        SetUpdatedAt();
    }

    public void Submit(Guid userId)
    {
        if (Status != EntryStatus.Draft && Status != EntryStatus.NeedsRevision)
            throw new InvalidOperationException("Entry can only be submitted from Draft or NeedsRevision status.");

        Status = EntryStatus.Submitted;
        SubmittedBy = userId;
        SubmittedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Approve(Guid userId)
    {
        if (Status != EntryStatus.Submitted)
            throw new InvalidOperationException("Entry can only be approved from Submitted status.");

        Status = EntryStatus.Approved;
        ApprovedBy = userId;
        ApprovedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Reject(Guid userId, string reason)
    {
        if (Status != EntryStatus.Submitted)
            throw new InvalidOperationException("Entry can only be rejected from Submitted status.");

        Status = EntryStatus.Rejected;
        ApprovedBy = userId;
        ApprovedAt = DateTime.UtcNow;
        RejectionReason = reason;
        SetUpdatedAt();
    }

    public void RequestRevision(Guid userId, string reason)
    {
        if (Status != EntryStatus.Submitted)
            throw new InvalidOperationException("Entry can only request revision from Submitted status.");

        Status = EntryStatus.NeedsRevision;
        ApprovedBy = userId;
        ApprovedAt = DateTime.UtcNow;
        RejectionReason = reason;
        SetUpdatedAt();
    }

    public EvidenceDocument AddEvidenceDocument(string fileName, long fileSize, string contentType, string storagePath, Guid uploadedBy)
    {
        var document = new EvidenceDocument(Id, fileName, fileSize, contentType, storagePath, uploadedBy);
        _evidenceDocuments.Add(document);
        SetUpdatedAt();
        return document;
    }

    public void RemoveEvidenceDocument(Guid documentId)
    {
        var document = _evidenceDocuments.FirstOrDefault(d => d.Id == documentId);
        if (document != null)
        {
            _evidenceDocuments.Remove(document);
            SetUpdatedAt();
        }
    }
}
