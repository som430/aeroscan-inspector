using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroScan.PointCloud
{
	/// <summary>
	/// Represents a single 3D point with optional intensity metadata.
	/// </summary>
	public record Point3D(double X, double Y, double Z, double Intensity = 0.0);
}
