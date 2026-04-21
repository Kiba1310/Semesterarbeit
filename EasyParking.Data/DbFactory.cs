using Microsoft.EntityFrameworkCore;

namespace EasyParking.Data;

public static class DbFactory
{
    public static DbContextOptions<EasyParkingDbContext> Options(string dbPath)
    {
        return new DbContextOptionsBuilder<EasyParkingDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
    }
}
