using System.Text;
using AeroScan.PointCloud;
using Xunit;

namespace AeroScan.PointCloud.Tests;

public class PointCloudParserTests
{
	private static Stream ToStream(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

	[Fact]
	public void Csv_SkipsCommentsAndInvalidRows()
	{
		const string csv = """
			# x,y,z,intensity
			0.0,0.0,0.0,1.0
			10.5,0,0.001
			bad,row,here
			1,2
			""";

		var pts = PointCloudParser.ParseCsv(ToStream(csv));

		Assert.Equal(2, pts.Count);
		Assert.Equal(new Point3D(10.5, 0, 0.001, 0), pts[1]);
		Assert.Equal(1.0, pts[0].Intensity);
	}

	[Fact]
	public void Ply_ReadsVerticesInDeclaredPropertyOrder()
	{
		const string ply = """
			ply
			format ascii 1.0
			element vertex 2
			property float z
			property float x
			property float y
			element face 0
			property list uchar int vertex_indices
			end_header
			3 1 2
			6 4 5
			""";

		var pts = PointCloudParser.ParsePly(ToStream(ply));

		Assert.Equal(2, pts.Count);
		Assert.Equal(new Point3D(1, 2, 3, 0), pts[0]);
		Assert.Equal(new Point3D(4, 5, 6, 0), pts[1]);
	}
}
