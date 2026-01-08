using FluentValidation;

namespace LibraHub.Catalog.Application.Announcements.Commands.UpdateAnnouncement;

public class UpdateAnnouncementValidator : AbstractValidator<UpdateAnnouncementCommand>
{
    public UpdateAnnouncementValidator()
    {
        RuleFor(x => x.AnnouncementId)
            .NotEmpty().WithMessage("Announcement ID is required");

        RuleFor(x => x.Title)
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.Content)
            .MaximumLength(10000).WithMessage("Content must not exceed 10000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Content));
    }
}

