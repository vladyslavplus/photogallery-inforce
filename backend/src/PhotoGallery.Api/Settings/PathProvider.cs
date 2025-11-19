using PhotoGallery.Application.Interfaces.Services;

namespace PhotoGallery.Api.Settings
{
    public class PathProvider : IPathProvider
    {
        private readonly IWebHostEnvironment _env;

        public PathProvider(IWebHostEnvironment env)
        {
            _env = env;
        }

        public string WebRootPath => _env.WebRootPath;
    }
}
