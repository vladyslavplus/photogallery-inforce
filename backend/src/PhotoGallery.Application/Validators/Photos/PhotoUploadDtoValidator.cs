using FluentValidation;
using PhotoGallery.Application.DTOs.Photos;

namespace PhotoGallery.Application.Validators.Photos
{
    public class PhotoUploadDtoValidator : AbstractValidator<PhotoUploadDto>
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 10 * 1024 * 1024;

        public PhotoUploadDtoValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("File is required")
                .Must(file => file != null && file.Length > 0).WithMessage("File cannot be empty")
                .Must(file => file == null || file.Length <= MaxFileSize)
                    .WithMessage($"File size must not exceed {MaxFileSize / 1024 / 1024} MB")
                .Must(file => file == null || IsValidExtension(file.FileName))
                    .WithMessage($"Invalid file type. Allowed: {string.Join(", ", AllowedExtensions)}");

            RuleFor(x => x.Title)
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.Title));

            RuleFor(x => x.AlbumId)
                .NotEmpty().WithMessage("Album ID is required");
        }

        private static bool IsValidExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }
    }
}
