using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScan.PointCloud
{
	/// <summary>
	/// CSV와 ASCII PLY 형식의 점군 데이터를 파싱합니다.
	/// </summary>
	public static class PointCloudParser
	{
		/// <summary>
		/// CSV 파싱: 각 줄은 x,y,z 또는 x,y,z,intensity
		/// '#'으로 시작하는 줄은 주석으로 처리
		/// </summary>
		public static List<Point3D> ParseCsv(Stream stream)
		{
			var points = new List<Point3D>();
			using var reader = new StreamReader(stream, leaveOpen: true);

			string? line;
			while ((line = reader.ReadLine()) != null)
			{
				line = line.Trim();
				if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
					continue;

				var parts = line.Split(',');
				if (parts.Length < 3)
					continue;

				if (!TryParseDouble(parts[0], out var x) ||
					!TryParseDouble(parts[1], out var y) ||
					!TryParseDouble(parts[2], out var z))
					continue;

				var intensity = parts.Length >= 4 && TryParseDouble(parts[3], out var i) ? i : 0.0;
				points.Add(new Point3D(x, y, z, intensity));
			}

			return points;
		}

		/// <summary>
		/// ASCII PLY 파싱. x, y, z vertex 속성만 읽습니다.
		/// </summary>
		public static List<Point3D> ParsePly(Stream stream)
		{
			var points = new List<Point3D>();
			using var reader = new StreamReader(stream, leaveOpen: true);

			int vertexCount = 0;
			var propertyOrder = new List<string>();
			bool inHeader = true;
			bool inVertexElement = false;

			while (inHeader)
			{
				var line = reader.ReadLine()?.Trim();
				if (line == null) break;
				if (line == "end_header") { inHeader = false; break; }

				if (line.StartsWith("element vertex"))
					int.TryParse(line.Split(' ')[^1], out vertexCount);

				if (line.StartsWith("element") && !line.StartsWith("element vertex"))
					inVertexElement = false;
				else if (line.StartsWith("element vertex"))
					inVertexElement = true;

				if (inVertexElement && line.StartsWith("property"))
				{
					var parts = line.Split(' ');
					if (parts.Length >= 3)
						propertyOrder.Add(parts[^1].ToLowerInvariant());
				}
			}

			int xIdx = propertyOrder.IndexOf("x");
			int yIdx = propertyOrder.IndexOf("y");
			int zIdx = propertyOrder.IndexOf("z");
			int iIdx = propertyOrder.IndexOf("intensity");

			if (xIdx < 0 || yIdx < 0 || zIdx < 0)
				return points;

			for (int v = 0; v < vertexCount; v++)
			{
				var line = reader.ReadLine()?.Trim();
				if (line == null) break;

				var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length <= Math.Max(xIdx, Math.Max(yIdx, zIdx)))
					continue;

				if (!TryParseDouble(parts[xIdx], out var x) ||
					!TryParseDouble(parts[yIdx], out var y) ||
					!TryParseDouble(parts[zIdx], out var z))
					continue;

				var intensity = iIdx >= 0 && iIdx < parts.Length && TryParseDouble(parts[iIdx], out var i) ? i : 0.0;
				points.Add(new Point3D(x, y, z, intensity));
			}

			return points;
		}

		private static bool TryParseDouble(string s, out double value) =>
			double.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out value);
	}
}
