using FluentValidation;

namespace LibraHub.Catalog.Application.Announcements.Commands.PublishAnnouncement;

public class PublishAnnouncementValidator : AbstractValidator<PublishAnnouncementCommand>
{
    public PublishAnnouncementValidator()
    {
        RuleFor(x => x.AnnouncementId)
            .NotEmpty().WithMessage("Announcement ID is required");
    }
}
