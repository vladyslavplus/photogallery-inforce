using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoGallery.Application.Common;
using PhotoGallery.Application.DTOs.Albums;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.ServiceDefaults.Extensions;

namespace PhotoGallery.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlbumsController : ControllerBase
    {
        private readonly IAlbumService _albumService;

        public AlbumsController(IAlbumService albumService)
        {
            _albumService = albumService;
        }

        /// <summary>
        /// Get all albums with pagination.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<AlbumResponseDto>), 200)]
        public async Task<IActionResult> GetAllAlbums([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5, CancellationToken cancellationToken = default)
        {
            var result = await _albumService.GetAllAlbumsAsync(pageNumber, pageSize, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get albums of the currently authenticated user with pagination.
        /// </summary>
        [HttpGet("my")]
        [Authorize]
        [ProducesResponseType(typeof(PagedResult<AlbumResponseDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetMyAlbums([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5, CancellationToken cancellationToken = default)
        {
            var userId = User.GetUserId();
            var result = await _albumService.GetUserAlbumsAsync(userId, pageNumber, pageSize, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get a specific album by its ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AlbumResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAlbumById(Guid id, CancellationToken cancellationToken = default)
        {
            var album = await _albumService.GetAlbumByIdAsync(id, cancellationToken);
            if (album == null)
                return NotFound(new { message = "Album not found" });

            return Ok(album);
        }

        /// <summary>
        /// Create a new album.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(AlbumResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateAlbum([FromBody] AlbumCreateDto dto, CancellationToken cancellationToken = default)
        {
            var userId = User.GetUserId();
            var album = await _albumService.CreateAlbumAsync(dto, userId, cancellationToken);
            return CreatedAtAction(nameof(GetAlbumById), new { id = album.Id }, album);
        }

        /// <summary>
        /// Update an existing album.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(AlbumResponseDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> UpdateAlbum(Guid id, [FromBody] AlbumUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var userId = User.GetUserId();
            var album = await _albumService.UpdateAlbumAsync(id, dto, userId, cancellationToken);
            if (album == null)
                return NotFound(new { message = "Album not found" });

            return Ok(album);
        }

        /// <summary>
        /// Delete an album by ID.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> DeleteAlbum(Guid id, CancellationToken cancellationToken = default)
        {
            var userId = User.GetUserId();
            var isAdmin = User.IsInRole("Admin");
            var deleted = await _albumService.DeleteAlbumAsync(id, userId, isAdmin, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "Album not found" });

            return Ok(new { message = "Album deleted successfully" });
        }
    }
}