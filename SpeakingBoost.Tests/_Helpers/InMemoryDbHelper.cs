using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;

namespace SpeakingBoost.Tests._Helpers
{
    /// <summary>
    /// Tạo ApplicationDbContext dùng InMemory database cho unit test.
    /// Mỗi test nên dùng tên database khác nhau để tránh dữ liệu bị chia sẻ.
    /// </summary>
    public static class InMemoryDbHelper
    {
        public static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
