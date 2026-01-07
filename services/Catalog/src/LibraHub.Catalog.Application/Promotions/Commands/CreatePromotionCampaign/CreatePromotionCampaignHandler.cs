using LibraHub.BuildingBlocks.Results;
using LibraHub.Catalog.Application.Abstractions;
using LibraHub.Catalog.Domain.Promotions;
using MediatR;

namespace LibraHub.Catalog.Application.Promotions.Commands.CreatePromotionCampaign;

public class CreatePromotionCampaignHandler(
    IPromotionRepository promotionRepository) : IRequestHandler<CreatePromotionCampaignCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePromotionCampaignCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var campaign = new PromotionCampaign(
                Guid.NewGuid(),
                request.Name,
                request.Description,
                request.StartsAtUtc.UtcDateTime,
                request.EndsAtUtc.UtcDateTime,
                request.StackingPolicy,
                request.Priority,
                request.CreatedBy);

            await promotionRepository.AddAsync(campaign, cancellationToken);

            var audit = new PromotionAudit(
                Guid.NewGuid(),
                campaign.Id,
                "Created",
                request.CreatedBy);
            await promotionRepository.AddAuditAsync(audit, cancellationToken);

            return Result.Success(campaign.Id);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>(Error.Validation(ex.Message));
        }
    }
}
