using AeroScan.API.Data;
using AeroScan.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AeroScanDbContext>(options =>
	options.UseSqlite(
		builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<InspectionService>();

// ── Web ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new()
	{
		Title = "AeroScan Inspector API",
		Version = "v1",
		Description = "Point cloud flatness inspection for aerospace manufacturing"
	});
});

// Blazor 프론트 허용 (CORS)
builder.Services.AddCors(opt =>
	opt.AddDefaultPolicy(policy =>
		policy.WithOrigins("http://localhost:5001", "https://localhost:7001", "https://localhost:7085")
              .AllowAnyHeader()
			  .AllowAnyMethod()));

var app = builder.Build();

// ── 시작 시 자동 마이그레이션 ─────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AeroScanDbContext>();
	db.Database.Migrate();
}

// ── Middleware ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AeroScan v1"));
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();