using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Errors;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Catalog.Application.Promotions.Queries.GetCampaign;

public class GetCampaignHandler(
    IPromotionRepository promotionRepository) : IRequestHandler<GetCampaignQuery, Result<GetCampaignResponseDto>>
{
    public async Task<Result<GetCampaignResponseDto>> Handle(GetCampaignQuery request, CancellationToken cancellationToken)
    {
        var campaign = await promotionRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign == null)
        {
            return Result.Failure<GetCampaignResponseDto>(Error.NotFound(CatalogErrors.Promotion.NotFound));
        }

        var rules = campaign.Rules.Select(r => new PromotionRuleDto
        {
            Id = r.Id,
            DiscountType = r.DiscountType.ToString(),
            DiscountValue = r.DiscountValue,
            Currency = r.Currency,
            MinPriceAfterDiscount = r.MinPriceAfterDiscount,
            MaxDiscountAmount = r.MaxDiscountAmount,
            AppliesToScope = r.AppliesToScope.ToString(),
            ScopeValues = r.ScopeValues,
            Exclusions = r.Exclusions,
            CreatedAt = new DateTimeOffset(r.CreatedAt, TimeSpan.Zero)
        }).ToList();

        var response = new GetCampaignResponseDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            Status = campaign.Status.ToString(),
            StartsAtUtc = new DateTimeOffset(campaign.StartsAtUtc, TimeSpan.Zero),
            EndsAtUtc = new DateTimeOffset(campaign.EndsAtUtc, TimeSpan.Zero),
            StackingPolicy = campaign.StackingPolicy.ToString(),
            Priority = campaign.Priority,
            CreatedBy = campaign.CreatedBy,
            CreatedAt = new DateTimeOffset(campaign.CreatedAt, TimeSpan.Zero),
            Rules = rules
        };

        return Result.Success(response);
    }
}
