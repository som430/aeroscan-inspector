namespace AeroScan.PointCloud;

/// <summary>
/// 배정밀도 3D 벡터. System.Numerics.Vector3(float)는 좌표값이 클 때
/// 마이크로미터 단위 편차가 반올림으로 사라지므로 사용하지 않는다.
/// </summary>
public readonly record struct Vector3D(double X, double Y, double Z)
{
	public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

	public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	public static Vector3D operator -(Vector3D a) => new(-a.X, -a.Y, -a.Z);
	public static Vector3D operator *(Vector3D a, double s) => new(a.X * s, a.Y * s, a.Z * s);

	public static double Dot(Vector3D a, Vector3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

	public Vector3D Normalize()
	{
		double len = Length;
		return len > 0 ? this * (1.0 / len) : this;
	}
}

/// <summary>
/// 점군에 대한 최적평면. 법선은 단위 벡터, 원점은 점군의 무게중심.
/// </summary>
public sealed class Plane3D
{
	/// <summary>
	/// 두 번째로 작은 고유값이 최대 고유값의 이 비율 이하이면
	/// 점들이 한 직선 위에 있다고 보고 평면을 정의하지 않는다.
	/// </summary>
	private const double CollinearRatio = 1e-12;

	public Vector3D Normal { get; }
	public Vector3D Origin { get; }

	/// <summary>공분산 행렬 고유값 (오름차순). 가장 작은 값이 법선 방향 분산.</summary>
	public IReadOnlyList<double> EigenValues { get; }

	public Plane3D(Vector3D normal, Vector3D origin) : this(normal, origin, Array.Empty<double>()) { }

	private Plane3D(Vector3D normal, Vector3D origin, double[] eigenValues)
	{
		if (normal.Length == 0)
			throw new ArgumentException("법선 벡터의 길이가 0입니다.", nameof(normal));

		Normal = normal.Normalize();
		Origin = origin;
		EigenValues = eigenValues;
	}

	/// <summary>
	/// 점에서 평면까지의 부호 있는 거리 (법선 방향이 양수).
	/// </summary>
	public double SignedDistance(Point3D p) =>
		Vector3D.Dot(Normal, new Vector3D(p.X, p.Y, p.Z) - Origin);

	/// <summary>
	/// PCA 기반 전체 최소자승(total least squares) 평면 피팅.
	/// <para>
	/// 1) 무게중심을 구해 점군을 원점으로 옮기고
	/// 2) 3×3 공분산 행렬을 만든 뒤
	/// 3) 가장 작은 고유값에 대응하는 고유벡터를 법선으로 사용한다.
	/// </para>
	/// 이 방법은 점과 평면 사이의 수직 거리 제곱합을 최소화하므로,
	/// Z = aX + bY + c 형태의 회귀와 달리 평면의 기울기(수직 평면 포함)에 관계없이 동작한다.
	/// </summary>
	/// <returns>점이 3개 미만이거나 모두 한 직선 위에 있으면 null.</returns>
	public static Plane3D? FitToPoints(IReadOnlyList<Point3D> points)
	{
		if (points.Count < 3)
			return null;

		int n = points.Count;
		double cx = 0, cy = 0, cz = 0;
		foreach (var p in points)
		{
			cx += p.X; cy += p.Y; cz += p.Z;
		}
		cx /= n; cy /= n; cz /= n;

		// 무게중심 기준 공분산 (n으로 나눈 모분산)
		double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
		foreach (var p in points)
		{
			double dx = p.X - cx, dy = p.Y - cy, dz = p.Z - cz;
			xx += dx * dx; xy += dx * dy; xz += dx * dz;
			yy += dy * dy; yz += dy * dz; zz += dz * dz;
		}

		double[,] cov =
		{
			{ xx / n, xy / n, xz / n },
			{ xy / n, yy / n, yz / n },
			{ xz / n, yz / n, zz / n }
		};

		var (values, vectors) = SymmetricEigen3x3(cov);

		// values는 오름차순. values[1]까지 0에 가까우면 점들이 직선 위에 있다.
		double scale = Math.Max(Math.Abs(values[2]), double.Epsilon);
		if (values[1] <= scale * CollinearRatio)
			return null;

		var normal = new Vector3D(vectors[0, 0], vectors[1, 0], vectors[2, 0]);
		normal = OrientDeterministically(normal.Normalize());

		return new Plane3D(normal, new Vector3D(cx, cy, cz), values);
	}

	/// <summary>
	/// 고유벡터의 부호는 임의이므로, 같은 입력에 항상 같은 법선이 나오도록 방향을 고정한다.
	/// 절댓값이 가장 큰 성분이 양수가 되게 한다 (거의 수평인 평면이면 +Z 방향).
	/// </summary>
	private static Vector3D OrientDeterministically(Vector3D n)
	{
		double ax = Math.Abs(n.X), ay = Math.Abs(n.Y), az = Math.Abs(n.Z);
		double dominant = az >= ax && az >= ay ? n.Z : (ay >= ax ? n.Y : n.X);
		return dominant < 0 ? -n : n;
	}

	/// <summary>
	/// 실대칭 3×3 행렬의 고유분해 (순환 Jacobi 회전).
	/// 반환: 오름차순 고유값, 그리고 같은 순서의 고유벡터(열 벡터).
	/// </summary>
	internal static (double[] Values, double[,] Vectors) SymmetricEigen3x3(double[,] matrix)
	{
		var a = (double[,])matrix.Clone();
		var v = new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

		for (int sweep = 0; sweep < 50; sweep++)
		{
			double off = a[0, 1] * a[0, 1] + a[0, 2] * a[0, 2] + a[1, 2] * a[1, 2];
			double diag = a[0, 0] * a[0, 0] + a[1, 1] * a[1, 1] + a[2, 2] * a[2, 2];
			if (off <= 1e-30 * Math.Max(diag, double.Epsilon))
				break;

			for (int p = 0; p < 2; p++)
			{
				for (int q = p + 1; q < 3; q++)
				{
					if (a[p, q] == 0.0)
						continue;

					double theta = (a[q, q] - a[p, p]) / (2.0 * a[p, q]);
					double t = Math.Sign(theta) / (Math.Abs(theta) + Math.Sqrt(theta * theta + 1.0));
					if (theta == 0.0) t = 1.0;
					double c = 1.0 / Math.Sqrt(t * t + 1.0);
					double s = t * c;

					// A' = Jᵀ A J
					for (int k = 0; k < 3; k++)
					{
						double akp = a[k, p], akq = a[k, q];
						a[k, p] = c * akp - s * akq;
						a[k, q] = s * akp + c * akq;
					}
					for (int k = 0; k < 3; k++)
					{
						double apk = a[p, k], aqk = a[q, k];
						a[p, k] = c * apk - s * aqk;
						a[q, k] = s * apk + c * aqk;
					}
					// V' = V J
					for (int k = 0; k < 3; k++)
					{
						double vkp = v[k, p], vkq = v[k, q];
						v[k, p] = c * vkp - s * vkq;
						v[k, q] = s * vkp + c * vkq;
					}
				}
			}
		}

		// 고유값 오름차순 정렬 (열 순서도 함께)
		int[] order = { 0, 1, 2 };
		Array.Sort(order, (i, j) => a[i, i].CompareTo(a[j, j]));

		var values = new double[3];
		var vectors = new double[3, 3];
		for (int col = 0; col < 3; col++)
		{
			int src = order[col];
			values[col] = a[src, src];
			for (int row = 0; row < 3; row++)
				vectors[row, col] = v[row, src];
		}

		return (values, vectors);
	}
}
