using System.Numerics;

namespace AeroScan.PointCloud;

/// <summary>
/// 최소자승법으로 피팅한 평면. Z = aX + bY + c 형태.
/// 항공 제조 검사처럼 표면이 거의 평탄한 경우에 수치적으로 안정적.
/// </summary>
public class Plane3D
{
	public Vector3 Normal { get; }
	public Vector3 Origin { get; }

	public Plane3D(Vector3 normal, Vector3 origin)
	{
		Normal = Vector3.Normalize(normal);
		Origin = origin;
	}

	/// <summary>
	/// 점에서 평면까지의 부호있는 거리 (양수=위, 음수=아래)
	/// </summary>
	public double SignedDistance(Point3D p)
	{
		var v = new Vector3((float)p.X, (float)p.Y, (float)p.Z) - Origin;
		return Vector3.Dot(Normal, v);
	}

	/// <summary>
	/// 최소자승법으로 점군의 최적평면 피팅.
	/// Z = aX + bY + c 로 풀고 법선벡터 (-a, -b, 1) 계산.
	/// </summary>
	public static Plane3D? FitToPoints(IReadOnlyList<Point3D> points)
	{
		if (points.Count < 3)
			return null;

		int n = points.Count;
		double sx = 0, sy = 0, sz = 0;
		double sx2 = 0, sy2 = 0, sxy = 0, sxz = 0, syz = 0;

		foreach (var p in points)
		{
			sx += p.X; sy += p.Y; sz += p.Z;
			sx2 += p.X * p.X;
			sy2 += p.Y * p.Y;
			sxy += p.X * p.Y;
			sxz += p.X * p.Z;
			syz += p.Y * p.Z;
		}

		// 정규방정식 [sx2 sxy sx] [a]   [sxz]
		//            [sxy sy2 sy] [b] = [syz]
		//            [sx  sy   n] [c]   [sz ]
		double[,] A =
		{
			{ sx2, sxy, sx },
			{ sxy, sy2, sy },
			{ sx,  sy,  n  }
		};
		double[] b = { sxz, syz, sz };

		var sol = SolveLinear3x3(A, b);
		if (sol is null)
			return null;

		double a = sol[0], bCoef = sol[1], c = sol[2];

		// 법선벡터: Z = aX + bY + c 의 법선은 (-a, -b, 1)
		var normal = new Vector3((float)-a, (float)-bCoef, 1f);

		// 원점: 무게중심을 평면에 투영
		double cx = sx / n, cy = sy / n;
		double cz = a * cx + bCoef * cy + c;
		var origin = new Vector3((float)cx, (float)cy, (float)cz);

		return new Plane3D(normal, origin);
	}

	/// <summary>
	/// 가우스 소거법으로 3x3 연립방정식 풀기
	/// </summary>
	private static double[]? SolveLinear3x3(double[,] A, double[] b)
	{
		// 확장행렬 만들기
		double[,] m = new double[3, 4];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++) m[i, j] = A[i, j];
			m[i, 3] = b[i];
		}

		// 전진 소거
		for (int col = 0; col < 3; col++)
		{
			// 피벗 선택 (부분 피벗팅)
			int pivot = col;
			for (int row = col + 1; row < 3; row++)
				if (Math.Abs(m[row, col]) > Math.Abs(m[pivot, col]))
					pivot = row;

			// 행 교환
			for (int j = 0; j <= 3; j++)
				(m[col, j], m[pivot, j]) = (m[pivot, j], m[col, j]);

			if (Math.Abs(m[col, col]) < 1e-12)
				return null; // 특이행렬

			// 소거
			for (int row = col + 1; row < 3; row++)
			{
				double factor = m[row, col] / m[col, col];
				for (int j = col; j <= 3; j++)
					m[row, j] -= factor * m[col, j];
			}
		}

		// 후진 대입
		double[] x = new double[3];
		for (int i = 2; i >= 0; i--)
		{
			x[i] = m[i, 3];
			for (int j = i + 1; j < 3; j++)
				x[i] -= m[i, j] * x[j];
			x[i] /= m[i, i];
		}

		return x;
	}
}