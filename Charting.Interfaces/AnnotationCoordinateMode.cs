namespace StockSharp.Charting;

/// <summary>
/// Enumeration constants to define the Coordinate mode used to place an annotation.
/// </summary>
public enum AnnotationCoordinateMode
{
	/// <summary>
	/// Absolute, requires that coordinates X1,Y1,X2,Y2 are data-values.
	/// </summary>
	Absolute,

	/// <summary>
	/// Relative, requires that coordinates X1,Y1,X2,Y2 are double values between 0.0 and 1.0.
	/// </summary>
	Relative,

	/// <summary>
	/// RelativeX, requires that coordinates X1,X2 are double values between 0.0 and 1.0, whereas Y1,Y2 are data-values.
	/// </summary>
	RelativeX,

	/// <summary>
	/// RelativeY, requires that coordinates Y1,Y2 are double values between 0.0 and 1.0, whereas X1,X2 are data-values.
	/// </summary>
	RelativeY
}