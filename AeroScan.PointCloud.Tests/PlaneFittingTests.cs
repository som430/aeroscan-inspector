using AeroScan.PointCloud;
using Xunit;

namespace AeroScan.PointCloud.Tests;

public class PlaneFittingTests
{
	private const double Eps = 1e-9;

	private static List<Point3D> Grid(Func<double, double, Point3D> map, int size = 5, double step = 10.0)
	{
		var pts = new List<Point3D>();
		for (int i = 0; i < size; i++)
			for (int j = 0; j < size; j++)
				pts.Add(map(i * step, j * step));
		return pts;
	}

	[Fact]
	public void HorizontalPlane_NormalIsPlusZ_AndFlatnessIsZero()
	{
		var pts = Grid((x, y) => new Point3D(x, y, 3.0));

		var plane = Plane3D.FitToPoints(pts)!;
		var result = GeometryInspector.Inspect(pts, 0.01);

		Assert.Equal(0.0, plane.Normal.X, Eps);
		Assert.Equal(0.0, plane.Normal.Y, Eps);
		Assert.Equal(1.0, plane.Normal.Z, Eps);
		Assert.Equal(0.0, result.FlatnessErrorMm, Eps);
		Assert.True(result.Passed);
	}

	[Fact]
	public void TiltedPlane_RecoversKnownNormal()
	{
		// 평면 2x - y + 4z = 7  →  z = (7 - 2x + y) / 4
		var pts = Grid((x, y) => new Point3D(x, y, (7 - 2 * x + y) / 4.0));
		var expected = new Vector3D(2, -1, 4).Normalize();

		var plane = Plane3D.FitToPoints(pts)!;

		Assert.Equal(expected.X, plane.Normal.X, Eps);
		Assert.Equal(expected.Y, plane.Normal.Y, Eps);
		Assert.Equal(expected.Z, plane.Normal.Z, Eps);
		Assert.All(pts, p => Assert.Equal(0.0, plane.SignedDistance(p), 1e-9));
	}

	[Fact]
	public void VerticalPlane_IsFitted()
	{
		// x = 5 인 수직 평면. Z = aX + bY + c 회귀로는 정의되지 않는 경우.
		var pts = Grid((y, z) => new Point3D(5.0, y, z));

		var plane = Plane3D.FitToPoints(pts);

		Assert.NotNull(plane);
		Assert.Equal(1.0, plane!.Normal.X, Eps);
		Assert.Equal(0.0, plane.Normal.Y, Eps);
		Assert.Equal(0.0, plane.Normal.Z, Eps);
	}

	[Fact]
	public void KnownDeviationPattern_GivesExactFlatness()
	{
		// 모서리 4점 +h, 변 중점 4점 -h, 중심 0.
		// x·z, y·z 상관이 0이고 z 평균도 0이라 최적평면은 z = 0, 평탄도는 정확히 2h.
		const double h = 0.1;
		var pts = new List<Point3D>
		{
			new(-1, -1, h), new(1, -1, h), new(-1, 1, h), new(1, 1, h),
			new(0, -1, -h), new(0, 1, -h), new(-1, 0, -h), new(1, 0, -h),
			new(0, 0, 0)
		};

		var result = GeometryInspector.Inspect(pts, toleranceMm: 0.15);

		Assert.Equal(2 * h, result.FlatnessErrorMm, 1e-12);
		Assert.Equal(h, result.MaxDeviationMm, 1e-12);
		Assert.Equal(-h, result.MinDeviationMm, 1e-12);
		Assert.False(result.Passed);           // 0.2 > 0.15
		Assert.Equal(0, result.OutOfToleranceCount); // 개별 점 |0.1| ≤ 0.15
	}

	[Fact]
	public void LargeCoordinates_KeepMicrometerDeviation()
	{
		// 원점에서 1,000 m 떨어진 부품. float(Vector3)로는 0.001 mm 편차가 반올림으로 사라지는 크기.
		const double offset = 1_000_000.0;
		var pts = Grid((x, y) => new Point3D(offset + x, offset + y, offset));
		pts[12] = pts[12] with { Z = offset + 0.001 };   // 중앙점만 1 µm 돌출

		var result = GeometryInspector.Inspect(pts, 0.01);

		Assert.Equal(0.001, result.FlatnessErrorMm, 1e-6);
	}

	[Fact]
	public void SameInput_GivesSameNormalSign()
	{
		var pts = Grid((x, y) => new Point3D(x, y, 0.2 * x - 0.1 * y));
		var reversed = Enumerable.Reverse(pts).ToList();

		var a = Plane3D.FitToPoints(pts)!;
		var b = Plane3D.FitToPoints(reversed)!;

		Assert.Equal(a.Normal.X, b.Normal.X, Eps);
		Assert.Equal(a.Normal.Y, b.Normal.Y, Eps);
		Assert.Equal(a.Normal.Z, b.Normal.Z, Eps);
		Assert.True(a.Normal.Z > 0);
	}

	[Fact]
	public void CollinearPoints_ReturnInsufficient()
	{
		var pts = Enumerable.Range(0, 10).Select(i => new Point3D(i, 2 * i, -i)).ToList();

		Assert.Null(Plane3D.FitToPoints(pts));

		var result = GeometryInspector.Inspect(pts);
		Assert.False(result.Sufficient);
		Assert.NotNull(result.FailureReason);
	}

	[Fact]
	public void FewerThanThreePoints_ReturnInsufficient()
	{
		var result = GeometryInspector.Inspect(new[] { new Point3D(0, 0, 0), new Point3D(1, 0, 0) });

		Assert.False(result.Sufficient);
		Assert.Equal(2, result.TotalPoints);
	}

	[Fact]
	public void EigenDecomposition_ReconstructsMatrix()
	{
		double[,] m = { { 4, 1, 2 }, { 1, 3, 0.5 }, { 2, 0.5, 5 } };

		var (values, vectors) = Plane3D.SymmetricEigen3x3(m);

		Assert.True(values[0] <= values[1] && values[1] <= values[2]);
		for (int r = 0; r < 3; r++)
			for (int c = 0; c < 3; c++)
			{
				double sum = 0;
				for (int k = 0; k < 3; k++)
					sum += vectors[r, k] * values[k] * vectors[c, k];
				Assert.Equal(m[r, c], sum, 1e-10);
			}
	}
}
