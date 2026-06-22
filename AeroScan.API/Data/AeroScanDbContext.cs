using AeroScan.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroScan.API.Data;

public class AeroScanDbContext : DbContext
{
	public AeroScanDbContext(DbContextOptions<AeroScanDbContext> options) : base(options) { }

	public DbSet<InspectionSession> InspectionSessions => Set<InspectionSession>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<InspectionSession>(e =>
		{
			e.HasKey(x => x.Id);
			e.Property(x => x.PartName).IsRequired().HasMaxLength(200);
			e.Property(x => x.PartNumber).IsRequired().HasMaxLength(100);
			e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending");
			e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
		});
	}
}