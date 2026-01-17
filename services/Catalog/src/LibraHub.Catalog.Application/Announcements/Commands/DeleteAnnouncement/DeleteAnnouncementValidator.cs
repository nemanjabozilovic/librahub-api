using FluentValidation;

namespace LibraHub.Catalog.Application.Announcements.Commands.DeleteAnnouncement;

public class DeleteAnnouncementValidator : AbstractValidator<DeleteAnnouncementCommand>
{
    public DeleteAnnouncementValidator()
    {
        RuleFor(x => x.AnnouncementId)
            .NotEmpty().WithMessage("Announcement ID is required");
    }
}
