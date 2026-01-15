namespace LibraHub.Catalog.Domain.Promotions;

public class PromotionAudit
{
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string? MetadataJson { get; private set; }

    protected PromotionAudit()
    { }

    public PromotionAudit(
        Guid id,
        Guid campaignId,
        string action,
        Guid actorUserId,
        string? metadataJson = null)
    {
        Id = id;
        CampaignId = campaignId;
        Action = action;
        ActorUserId = actorUserId;
        AtUtc = DateTime.UtcNow;
        MetadataJson = metadataJson;
    }
}
