using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoGallery.Application.Common;
using PhotoGallery.Application.DTOs.Photos;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.ServiceDefaults.Extensions;

namespace PhotoGallery.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotosController : ControllerBase
    {
        private readonly IPhotoService _photoService;

        public PhotosController(IPhotoService photoService)
        {
            _photoService = photoService;
        }

        /// <summary>
        /// Get all photos from album (public, paginated)
        /// </summary>
        [HttpGet("album/{albumId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<PhotoResponseDto>), 200)]
        public async Task<IActionResult> GetAlbumPhotos(Guid albumId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5, CancellationToken cancellationToken = default)
        {
            var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var result = await _photoService.GetAlbumPhotosAsync(albumId, pageNumber, pageSize, userId, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get photo by ID (public)
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PhotoResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPhotoById(Guid id, CancellationToken cancellationToken = default)
        {
            var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var photo = await _photoService.GetPhotoByIdAsync(id, userId, cancellationToken);
            if (photo == null)
                return NotFound(new { message = "Photo not found" });
            return Ok(photo);
        }

        /// <summary>
        /// Upload photo to album (authorized, multipart/form-data)
        /// </summary>
        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(PhotoResponseDto), 201)]
        public async Task<IActionResult> UploadPhoto([FromForm] PhotoUploadDto dto, CancellationToken cancellationToken = default)
        {
            var userId = User.GetUserId();
            var photo = await _photoService.UploadPhotoAsync(dto, userId, cancellationToken);
            return CreatedAtAction(nameof(GetPhotoById), new { id = photo.Id }, photo);
        }

        /// <summary>
        /// Delete photo (owner or admin)
        /// </summary>
        [Authorize]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePhoto(Guid id, CancellationToken cancellationToken = default)
        {
            var userId = User.GetUserId();
            var isAdmin = User.IsInRole("Admin");
            var deleted = await _photoService.DeletePhotoAsync(id, userId, isAdmin, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "Photo not found" });
            return Ok(new { message = "Photo deleted successfully" });
        }

        /// <summary>
        /// Like photo (authorized)
        /// </summary>
        [Authorize]
        [HttpPost("{id:guid}/like")]
        [ProducesResponseType(typeof(PhotoResponseDto), 200)]
        public async Task<IActionResult> LikePhoto(Guid id, CancellationToken cancellationToken = default)
        {
            var userId = User.GetUserId();
            var photo = await _photoService.ToggleLikeAsync(id, userId, true, cancellationToken);
            return Ok(photo);
        }

        /// <summary>
        /// Dislike photo (authorized)
        /// </summary>
        [Authorize]
        [HttpPost("{id:guid}/dislike")]
        [ProducesResponseType(typeof(PhotoResponseDto), 200)]
        public async Task<IActionResult> DislikePhoto(Guid id, CancellationToken cancellationToken = default)
        {
            var userId = User.GetUserId();
            var photo = await _photoService.ToggleLikeAsync(id, userId, false, cancellationToken);
            return Ok(photo);
        }
    }
}