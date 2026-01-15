namespace LibraHub.Catalog.Domain.Promotions;

public class PromotionCampaign
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PromotionStatus Status { get; private set; }
    public DateTime StartsAtUtc { get; private set; }
    public DateTime EndsAtUtc { get; private set; }
    public StackingPolicy StackingPolicy { get; private set; }
    public int Priority { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<PromotionRule> _rules = new();
    public virtual IReadOnlyCollection<PromotionRule> Rules => _rules.AsReadOnly();

    protected PromotionCampaign()
    { }

    public PromotionCampaign(
        Guid id,
        string name,
        string? description,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        StackingPolicy stackingPolicy,
        int priority,
        Guid createdBy)
    {
        Id = id;
        Name = name;
        Description = description;
        Status = PromotionStatus.Draft;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        StackingPolicy = stackingPolicy;
        Priority = priority;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Campaign name is required", nameof(Name));
        }

        if (EndsAtUtc <= StartsAtUtc)
        {
            throw new ArgumentException("End date must be after start date", nameof(EndsAtUtc));
        }
    }

    public void Update(string name, string? description, DateTime startsAtUtc, DateTime endsAtUtc, StackingPolicy stackingPolicy, int priority)
    {
        if (Status != PromotionStatus.Draft && Status != PromotionStatus.Scheduled)
        {
            throw new InvalidOperationException("Can only update draft or scheduled campaigns");
        }

        Name = name;
        Description = description;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        StackingPolicy = stackingPolicy;
        Priority = priority;

        Validate();
    }

    public void Schedule()
    {
        if (Status != PromotionStatus.Draft)
        {
            throw new InvalidOperationException("Can only schedule draft campaigns");
        }

        Status = PromotionStatus.Scheduled;
    }

    public void Activate(DateTime utcNow)
    {
        if (Status != PromotionStatus.Scheduled && Status != PromotionStatus.Paused)
        {
            throw new InvalidOperationException("Can only activate scheduled or paused campaigns");
        }

        if (utcNow < StartsAtUtc)
        {
            throw new InvalidOperationException("Cannot activate campaign before start date");
        }

        if (utcNow > EndsAtUtc)
        {
            throw new InvalidOperationException("Cannot activate campaign after end date");
        }

        Status = PromotionStatus.Active;
    }

    public void Pause()
    {
        if (Status != PromotionStatus.Active)
        {
            throw new InvalidOperationException("Can only pause active campaigns");
        }

        Status = PromotionStatus.Paused;
    }

    public void End()
    {
        if (Status == PromotionStatus.Ended || Status == PromotionStatus.Cancelled)
        {
            throw new InvalidOperationException("Campaign is already ended or cancelled");
        }

        Status = PromotionStatus.Ended;
    }

    public void Cancel()
    {
        if (Status == PromotionStatus.Ended || Status == PromotionStatus.Cancelled)
        {
            throw new InvalidOperationException("Campaign is already ended or cancelled");
        }

        Status = PromotionStatus.Cancelled;
    }

    public void AddRule(PromotionRule rule)
    {
        if (Status == PromotionStatus.Ended || Status == PromotionStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot add rules to ended or cancelled campaigns");
        }

        if (rule.CampaignId != Id)
        {
            throw new ArgumentException("Rule does not belong to this campaign", nameof(rule));
        }

        _rules.Add(rule);
    }

    public void RemoveRule(Guid ruleId)
    {
        if (Status == PromotionStatus.Ended || Status == PromotionStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot remove rules from ended or cancelled campaigns");
        }

        var rule = _rules.FirstOrDefault(r => r.Id == ruleId);
        if (rule == null)
        {
            throw new InvalidOperationException("Rule not found");
        }

        _rules.Remove(rule);
    }

    public bool IsActive(DateTime utcNow)
    {
        return Status == PromotionStatus.Active
            && utcNow >= StartsAtUtc
            && utcNow <= EndsAtUtc;
    }
}
