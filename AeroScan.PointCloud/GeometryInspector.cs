using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScan.PointCloud;

/// <summary>
/// 점군과 기준 평면 사이의 편차 통계를 계산합니다.
/// 항공 제조 검사의 CMM 평탄도 측정과 동일한 방법론.
/// </summary>
public static class GeometryInspector
{
	/// <summary>
	/// 최적평면 피팅 후 모든 점의 부호있는 편차를 계산합니다.
	/// </summary>
	public static InspectionResult Inspect(
		IReadOnlyList<Point3D> points,
		double toleranceMm = 0.5)
	{
		if (points.Count < 3)
			return InspectionResult.Insufficient(points.Count);

		var plane = Plane3D.FitToPoints(points)!;

		var deviations = points
			.Select(p => plane.SignedDistance(p))
			.ToList();

		double minDev = deviations.Min();
		double maxDev = deviations.Max();
		double avgDev = deviations.Average();
		double rmsDev = Math.Sqrt(deviations.Average(d => d * d));
		double flatness = maxDev - minDev; // ISO 1101 평탄도 오차

		var pointResults = points
			.Zip(deviations, (p, d) => new PointDeviation(p, d, Math.Abs(d) > toleranceMm))
			.ToList();

		int outOfTolerance = pointResults.Count(r => r.OutOfTolerance);

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
			OutOfToleranceCount = outOfTolerance,
			Passed = flatness <= toleranceMm,
			PointDeviations = pointResults
		};
	}
}

public class InspectionResult
{
	public bool Sufficient { get; init; } = true;
	public int TotalPoints { get; init; }
	public float PlaneNormalX { get; init; }
	public float PlaneNormalY { get; init; }
	public float PlaneNormalZ { get; init; }
	public double MinDeviationMm { get; init; }
	public double MaxDeviationMm { get; init; }
	public double MeanDeviationMm { get; init; }
	public double RmsDeviationMm { get; init; }
	public double FlatnessErrorMm { get; init; }
	public double ToleranceMm { get; init; }
	public int OutOfToleranceCount { get; init; }
	public bool Passed { get; init; }
	public List<PointDeviation> PointDeviations { get; init; } = [];

	public static InspectionResult Insufficient(int count) => new()
	{
		Sufficient = false,
		TotalPoints = count
	};
}

public record PointDeviation(Point3D Point, double DeviationMm, bool OutOfTolerance);