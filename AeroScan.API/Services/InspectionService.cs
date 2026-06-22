using System.Text.Json;
using AeroScan.API.Data;
using AeroScan.API.DTOs;
using AeroScan.API.Models;
using AeroScan.PointCloud;
using Microsoft.EntityFrameworkCore;

namespace AeroScan.API.Services;

public class InspectionService
{
	private readonly AeroScanDbContext _db;
	private readonly ILogger<InspectionService> _logger;

	public InspectionService(AeroScanDbContext db, ILogger<InspectionService> logger)
	{
		_db = db;
		_logger = logger;
	}

	public async Task<SessionSummaryDto?> GetSessionAsync(int id)
	{
		var session = await _db.InspectionSessions.FindAsync(id);
		return session is null ? null : ToSummary(session);
	}

	public async Task<SessionDetailDto?> GetSessionDetailAsync(int id)
	{
		var session = await _db.InspectionSessions.FindAsync(id);
		if (session is null) return null;

		var dto = new SessionDetailDto
		{
			Id = session.Id,
			PartName = session.PartName,
			PartNumber = session.PartNumber,
			FileName = session.FileName,
			PointCount = session.PointCount,
			ToleranceMm = session.ToleranceMm,
			Passed = session.Passed,
			FlatnessErrorMm = session.FlatnessErrorMm,
			MinDeviationMm = session.MinDeviationMm,
			MaxDeviationMm = session.MaxDeviationMm,
			RmsDeviationMm = session.RmsDeviationMm,
			OutOfToleranceCount = session.OutOfToleranceCount,
			CreatedAt = session.CreatedAt,
			Status = session.Status
		};

		if (!string.IsNullOrEmpty(session.DeviationSnapshotJson))
		{
			dto.DeviationSnapshot = JsonSerializer.Deserialize<List<PointDeviationDto>>(
				session.DeviationSnapshotJson) ?? [];
		}

		return dto;
	}

	public async Task<List<SessionSummaryDto>> GetAllSessionsAsync(int skip = 0, int take = 50)
	{
		return await _db.InspectionSessions
			.OrderByDescending(s => s.CreatedAt)
			.Skip(skip).Take(take)
			.Select(s => new SessionSummaryDto
			{
				Id = s.Id,
				PartName = s.PartName,
				PartNumber = s.PartNumber,
				FileName = s.FileName,
				PointCount = s.PointCount,
				ToleranceMm = s.ToleranceMm,
				Passed = s.Passed,
				FlatnessErrorMm = s.FlatnessErrorMm,
				MinDeviationMm = s.MinDeviationMm,
				MaxDeviationMm = s.MaxDeviationMm,
				RmsDeviationMm = s.RmsDeviationMm,
				OutOfToleranceCount = s.OutOfToleranceCount,
				CreatedAt = s.CreatedAt,
				Status = s.Status
			})
			.ToListAsync();
	}

	public async Task<InspectionSession> CreateSessionAsync(CreateSessionRequest req)
	{
		var session = new InspectionSession
		{
			PartName = req.PartName,
			PartNumber = req.PartNumber,
			ToleranceMm = req.ToleranceMm,
			Status = "Pending"
		};

		_db.InspectionSessions.Add(session);
		await _db.SaveChangesAsync();
		return session;
	}

	public async Task<InspectionResultDto> RunInspectionAsync(
		int sessionId, Stream fileStream, string fileName)
	{
		var session = await _db.InspectionSessions.FindAsync(sessionId)
			?? throw new KeyNotFoundException($"Session {sessionId} not found.");

		session.FileName = fileName;
		session.Status = "Running";
		await _db.SaveChangesAsync();

		try
		{
			var ext = Path.GetExtension(fileName).ToLowerInvariant();
			List<Point3D> points = ext switch
			{
				".ply" => PointCloudParser.ParsePly(fileStream),
				".csv" => PointCloudParser.ParseCsv(fileStream),
				_ => throw new NotSupportedException($"'{ext}' 형식은 지원하지 않습니다. .csv 또는 .ply를 사용하세요.")
			};

			session.PointCount = points.Count;

			var result = GeometryInspector.Inspect(points, session.ToleranceMm);

			if (!result.Sufficient)
			{
				session.Status = "Failed";
				await _db.SaveChangesAsync();
				throw new InvalidOperationException("점이 3개 미만입니다.");
			}

			session.Passed = result.Passed;
			session.FlatnessErrorMm = result.FlatnessErrorMm;
			session.MinDeviationMm = result.MinDeviationMm;
			session.MaxDeviationMm = result.MaxDeviationMm;
			session.RmsDeviationMm = result.RmsDeviationMm;
			session.OutOfToleranceCount = result.OutOfToleranceCount;
			session.Status = "Completed";

			var snapshot = result.PointDeviations
				.Take(500)
				.Select(pd => new PointDeviationDto
				{
					X = pd.Point.X,
					Y = pd.Point.Y,
					Z = pd.Point.Z,
					DeviationMm = pd.DeviationMm,
					OutOfTolerance = pd.OutOfTolerance
				})
				.ToList();

			session.DeviationSnapshotJson = JsonSerializer.Serialize(snapshot);
			await _db.SaveChangesAsync();

			return new InspectionResultDto
			{
				Passed = result.Passed,
				TotalPoints = result.TotalPoints,
				FlatnessErrorMm = result.FlatnessErrorMm,
				MinDeviationMm = result.MinDeviationMm,
				MaxDeviationMm = result.MaxDeviationMm,
				RmsDeviationMm = result.RmsDeviationMm,
				OutOfToleranceCount = result.OutOfToleranceCount,
				ToleranceMm = session.ToleranceMm
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Inspection failed for session {SessionId}", sessionId);
			session.Status = "Failed";
			await _db.SaveChangesAsync();
			throw;
		}
	}

	public async Task<bool> DeleteSessionAsync(int id)
	{
		var session = await _db.InspectionSessions.FindAsync(id);
		if (session is null) return false;

		_db.InspectionSessions.Remove(session);
		await _db.SaveChangesAsync();
		return true;
	}

	private static SessionSummaryDto ToSummary(InspectionSession s) => new()
	{
		Id = s.Id,
		PartName = s.PartName,
		PartNumber = s.PartNumber,
		FileName = s.FileName,
		PointCount = s.PointCount,
		ToleranceMm = s.ToleranceMm,
		Passed = s.Passed,
		FlatnessErrorMm = s.FlatnessErrorMm,
		MinDeviationMm = s.MinDeviationMm,
		MaxDeviationMm = s.MaxDeviationMm,
		RmsDeviationMm = s.RmsDeviationMm,
		OutOfToleranceCount = s.OutOfToleranceCount,
		CreatedAt = s.CreatedAt,
		Status = s.Status
	};
}