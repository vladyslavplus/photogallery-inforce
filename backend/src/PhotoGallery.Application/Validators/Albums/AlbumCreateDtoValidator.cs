using FluentValidation;
using PhotoGallery.Application.DTOs.Albums;

namespace PhotoGallery.Application.Validators.Albums
{
    public class AlbumCreateDtoValidator : AbstractValidator<AlbumCreateDto>
    {
        public AlbumCreateDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}
