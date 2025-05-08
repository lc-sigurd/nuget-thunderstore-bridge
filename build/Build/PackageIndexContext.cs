using Build.Schema.Thunderstore;
using Microsoft.EntityFrameworkCore;

namespace Build;

public class PackageIndexContext : DbContext
{
    public DbSet<ThunderstorePackage> Packages { get; set; }
    public DbSet<ThunderstorePackageVersion> PackageVersions { get; set; }

    public required string DbPath { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}
