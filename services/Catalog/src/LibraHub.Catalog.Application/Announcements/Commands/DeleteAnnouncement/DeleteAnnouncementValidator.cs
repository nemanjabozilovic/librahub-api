using FluentValidation;

namespace LibraHub.Catalog.Application.Announcements.Commands.DeleteAnnouncement;

public class DeleteAnnouncementValidator : AbstractValidator<DeleteAnnouncementCommand>
{
    public DeleteAnnouncementValidator()
    {
        RuleFor(x => x.AnnouncementIds)
            .NotEmpty().WithMessage("At least one announcement ID is required");

        RuleForEach(x => x.AnnouncementIds)
            .NotEmpty().WithMessage("Announcement ID cannot be empty");
    }
}
