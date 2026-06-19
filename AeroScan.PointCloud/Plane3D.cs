using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace AeroScan.PointCloud
{
	
	/// <summary>
	/// 단위 법선벡터와 평면 위의 한 점으로 정의되는 무한 평면.
	/// 방정식: Normal · (P - Origin) = 0
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
		/// PCA 공분산행렬로 점군의 최적평면 피팅.
		/// </summary>
		public static Plane3D? FitToPoints(IReadOnlyList<Point3D> points)
		{
			if (points.Count < 3)
				return null;

			// 무게중심
			double cx = 0, cy = 0, cz = 0;
			foreach (var p in points) { cx += p.X; cy += p.Y; cz += p.Z; }
			cx /= points.Count; cy /= points.Count; cz /= points.Count;

			// 공분산 행렬 (대칭 3x3)
			double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
			foreach (var p in points)
			{
				double dx = p.X - cx, dy = p.Y - cy, dz = p.Z - cz;
				xx += dx * dx; xy += dx * dy; xz += dx * dz;
				yy += dy * dy; yz += dy * dz; zz += dz * dz;
			}

			var normal = SmallestEigenvector(xx, xy, xz, yy, yz, zz);
			var origin = new Vector3((float)cx, (float)cy, (float)cz);

			return new Plane3D(normal, origin);
		}

		private static Vector3 SmallestEigenvector(
			double xx, double xy, double xz,
			double yy, double yz, double zz)
		{
			var r0 = new Vector3((float)(yy * zz - yz * yz), (float)(xz * yz - xy * zz), (float)(xy * yz - xz * yy));
			var r1 = new Vector3((float)(xz * yz - xy * zz), (float)(xx * zz - xz * xz), (float)(xy * xz - yz * xx));
			var r2 = new Vector3((float)(xy * yz - xz * yy), (float)(xy * xz - yz * xx), (float)(xx * yy - xy * xy));

			var best = r0.LengthSquared() >= r1.LengthSquared()
				? (r0.LengthSquared() >= r2.LengthSquared() ? r0 : r2)
				: (r1.LengthSquared() >= r2.LengthSquared() ? r1 : r2);

			var candidate = Vector3.Cross(
				r0.LengthSquared() > r1.LengthSquared() ? r0 : r1,
				r0.LengthSquared() > r2.LengthSquared() ? r2 : r0);

			return candidate.LengthSquared() > 0
				? Vector3.Normalize(candidate)
				: Vector3.Normalize(best);
		}
	}
}
