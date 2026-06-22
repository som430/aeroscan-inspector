namespace AeroScan.API.DTOs;

// ── 요청 ──────────────────────────────────────────────────────────────────────

public class CreateSessionRequest
{
	public string PartName { get; set; } = string.Empty;
	public string PartNumber { get; set; } = string.Empty;
	public double ToleranceMm { get; set; } = 0.5;
}

// ── 응답 ──────────────────────────────────────────────────────────────────────

public class SessionSummaryDto
{
	public int Id { get; set; }
	public string PartName { get; set; } = string.Empty;
	public string PartNumber { get; set; } = string.Empty;
	public string FileName { get; set; } = string.Empty;
	public int PointCount { get; set; }
	public double ToleranceMm { get; set; }
	public bool? Passed { get; set; }
	public double? FlatnessErrorMm { get; set; }
	public double? MinDeviationMm { get; set; }
	public double? MaxDeviationMm { get; set; }
	public double? RmsDeviationMm { get; set; }
	public int? OutOfToleranceCount { get; set; }
	public DateTime CreatedAt { get; set; }
	public string Status { get; set; } = string.Empty;
}

public class SessionDetailDto : SessionSummaryDto
{
	/// <summary>히트맵 렌더링용 샘플 편차 데이터 (최대 500점)</summary>
	public List<PointDeviationDto> DeviationSnapshot { get; set; } = [];
}

public class PointDeviationDto
{
	public double X { get; set; }
	public double Y { get; set; }
	public double Z { get; set; }
	public double DeviationMm { get; set; }
	public bool OutOfTolerance { get; set; }
}

public class InspectionResultDto
{
	public bool Passed { get; set; }
	public int TotalPoints { get; set; }
	public double FlatnessErrorMm { get; set; }
	public double MinDeviationMm { get; set; }
	public double MaxDeviationMm { get; set; }
	public double RmsDeviationMm { get; set; }
	public int OutOfToleranceCount { get; set; }
	public double ToleranceMm { get; set; }
}