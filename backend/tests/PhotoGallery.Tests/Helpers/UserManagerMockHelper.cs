using Microsoft.AspNetCore.Identity;
using Moq;
using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Tests.Helpers
{
    public static class UserManagerMockHelper
    {
        public static Mock<UserManager<User>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            var userManager = new Mock<UserManager<User>>(
                store.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            return userManager;
        }
    }
}
