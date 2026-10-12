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

// Blazor 프론트 허용 (CORS) — 로컬 실행(launchSettings)과 docker-compose(8081) 주소
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
	?? new[] { "https://localhost:7085", "http://localhost:5048", "http://localhost:8081" };
builder.Services.AddCors(opt =>
	opt.AddDefaultPolicy(policy =>
		policy.WithOrigins(allowedOrigins)
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