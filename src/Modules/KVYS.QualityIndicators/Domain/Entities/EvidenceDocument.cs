using KVYS.Shared.Domain.Primitives;

namespace KVYS.QualityIndicators.Domain.Entities;

/// <summary>
/// Evidence document attached to an indicator entry.
/// </summary>
public class EvidenceDocument : Entity
{
    private EvidenceDocument() { }

    public EvidenceDocument(
        Guid indicatorEntryId,
        string fileName,
        long fileSize,
        string contentType,
        string storagePath,
        Guid uploadedBy)
        : base(Guid.NewGuid())
    {
        IndicatorEntryId = indicatorEntryId;
        FileName = fileName;
        FileSize = fileSize;
        ContentType = contentType;
        StoragePath = storagePath;
        UploadedBy = uploadedBy;
    }

    public Guid IndicatorEntryId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public Guid UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    public virtual IndicatorEntry IndicatorEntry { get; private set; } = null!;
}
