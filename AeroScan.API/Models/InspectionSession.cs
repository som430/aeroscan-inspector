using System.ComponentModel.DataAnnotations;

namespace AeroScan.API.Models;

public class InspectionSession
{
	public int Id { get; set; }

	[Required, MaxLength(200)]
	public string PartName { get; set; } = string.Empty;

	[Required, MaxLength(100)]
	public string PartNumber { get; set; } = string.Empty;

	public string FileName { get; set; } = string.Empty;
	public int PointCount { get; set; }
	public double ToleranceMm { get; set; }

	// 검사 결과 요약
	public bool? Passed { get; set; }
	public double? FlatnessErrorMm { get; set; }
	public double? MinDeviationMm { get; set; }
	public double? MaxDeviationMm { get; set; }
	public double? RmsDeviationMm { get; set; }
	public int? OutOfToleranceCount { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public string Status { get; set; } = "Pending"; // Pending | Completed | Failed

	// 편차 스냅샷 JSON (UI용 최대 500점)
	public string? DeviationSnapshotJson { get; set; }
}