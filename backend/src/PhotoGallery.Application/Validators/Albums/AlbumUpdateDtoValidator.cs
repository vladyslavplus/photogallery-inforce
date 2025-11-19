using FluentValidation;
using PhotoGallery.Application.DTOs.Albums;

namespace PhotoGallery.Application.Validators.Albums
{
    public class AlbumUpdateDtoValidator : AbstractValidator<AlbumUpdateDto>
    {
        public AlbumUpdateDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title cannot be empty if provided")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters")
                .When(x => x.Title != null);

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
                .When(x => x.Description != null);

            RuleFor(x => x)
                .Must(x => x.Title != null || x.Description != null)
                .WithMessage("At least one field must be provided");
        }
    }
}
