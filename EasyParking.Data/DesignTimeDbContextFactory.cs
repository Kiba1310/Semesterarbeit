using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EasyParking.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EasyParkingDbContext>
{
    public EasyParkingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EasyParkingDbContext>()
            .UseSqlite("Data Source=easyparking-design.db")
            .Options;
        return new EasyParkingDbContext(options);
    }
}
