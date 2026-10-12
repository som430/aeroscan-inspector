namespace AeroScan.PointCloud;

/// <summary>
/// 점군과 최적평면 사이의 편차 통계를 계산합니다.
/// </summary>
/// <remarks>
/// 평탄도는 PCA 최소자승 평면을 기준으로 한 편차 범위(max − min)입니다.
/// ISO 1101의 평탄도 정의(두 평행 평면 사이 최소 간격, minimum zone)와는 기준 평면 결정 방식이 다르며,
/// 최소자승 기준 값은 최소영역 평탄도보다 작아질 수 없으므로 판정은 보수적(불합격 쪽)으로 치우칩니다.
/// </remarks>
public static class GeometryInspector
{
	/// <summary>
	/// 최적평면 피팅 후 모든 점의 부호 있는 편차를 계산합니다.
	/// </summary>
	public static InspectionResult Inspect(
		IReadOnlyList<Point3D> points,
		double toleranceMm = 0.5)
	{
		if (points.Count < 3)
			return InspectionResult.Insufficient(points.Count, "점이 3개 미만입니다.");

		var plane = Plane3D.FitToPoints(points);
		if (plane is null)
			return InspectionResult.Insufficient(points.Count, "모든 점이 한 직선 위에 있어 평면을 정의할 수 없습니다.");

		var deviations = new double[points.Count];
		for (int i = 0; i < points.Count; i++)
			deviations[i] = plane.SignedDistance(points[i]);

		double minDev = deviations.Min();
		double maxDev = deviations.Max();
		double avgDev = deviations.Average();
		double rmsDev = Math.Sqrt(deviations.Average(d => d * d));
		double flatness = maxDev - minDev;

		var pointResults = new List<PointDeviation>(points.Count);
		for (int i = 0; i < points.Count; i++)
			pointResults.Add(new PointDeviation(points[i], deviations[i], Math.Abs(deviations[i]) > toleranceMm));

		return new InspectionResult
		{
			TotalPoints = points.Count,
			PlaneNormalX = plane.Normal.X,
			PlaneNormalY = plane.Normal.Y,
			PlaneNormalZ = plane.Normal.Z,
			MinDeviationMm = minDev,
			MaxDeviationMm = maxDev,
			MeanDeviationMm = avgDev,
			RmsDeviationMm = rmsDev,
			FlatnessErrorMm = flatness,
			ToleranceMm = toleranceMm,
			OutOfToleranceCount = pointResults.Count(r => r.OutOfTolerance),
			Passed = flatness <= toleranceMm,
			PointDeviations = pointResults
		};
	}
}

public class InspectionResult
{
	public bool Sufficient { get; init; } = true;
	public string? FailureReason { get; init; }
	public int TotalPoints { get; init; }
	public double PlaneNormalX { get; init; }
	public double PlaneNormalY { get; init; }
	public double PlaneNormalZ { get; init; }
	public double MinDeviationMm { get; init; }
	public double MaxDeviationMm { get; init; }
	/// <summary>최소자승 평면 기준이므로 이론상 0 (수치 오차 수준).</summary>
	public double MeanDeviationMm { get; init; }
	public double RmsDeviationMm { get; init; }
	/// <summary>최소자승 평면 기준 편차 범위 (max − min).</summary>
	public double FlatnessErrorMm { get; init; }
	public double ToleranceMm { get; init; }
	public int OutOfToleranceCount { get; init; }
	public bool Passed { get; init; }
	public List<PointDeviation> PointDeviations { get; init; } = [];

	public static InspectionResult Insufficient(int count, string reason) => new()
	{
		Sufficient = false,
		FailureReason = reason,
		TotalPoints = count
	};
}

public record PointDeviation(Point3D Point, double DeviationMm, bool OutOfTolerance);
