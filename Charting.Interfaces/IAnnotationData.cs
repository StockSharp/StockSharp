namespace StockSharp.Charting;

/// <summary>
/// Used to transfer annotation draw data.
/// </summary>
public interface IAnnotationData
{
	/// <summary>Show/hide annotation.</summary>
	bool? IsVisible { get; set; }

	/// <summary>Whether user can edit annotation.</summary>
	bool? IsEditable { get; set; }

	/// <summary>
	/// X1 coordinate for annotation drawing.
	/// <see cref="DateTimeOffset"/> for coordinate mode <see cref="AnnotationCoordinateMode.Absolute"/> or <see cref="AnnotationCoordinateMode.RelativeY"/>.
	/// <see cref="double"/> otherwise.
	/// </summary>
	IComparable X1 { get; set; }

	/// <summary>
	/// Y1 coordinate for annotation drawing.
	/// <see cref="decimal"/> for coordinate mode <see cref="AnnotationCoordinateMode.Absolute"/> or <see cref="AnnotationCoordinateMode.RelativeX"/>.
	/// <see cref="double"/> otherwise.
	/// </summary>
	IComparable Y1 { get; set; }

	/// <summary>
	/// X2 coordinate for annotation drawing.
	/// <see cref="DateTimeOffset"/> for coordinate mode <see cref="AnnotationCoordinateMode.Absolute"/> or <see cref="AnnotationCoordinateMode.RelativeY"/>.
	/// <see cref="double"/> otherwise.
	/// </summary>
	IComparable X2 { get; set; }

	/// <summary>
	/// Y2 coordinate for annotation drawing.
	/// <see cref="decimal"/> for coordinate mode <see cref="AnnotationCoordinateMode.Absolute"/> or <see cref="AnnotationCoordinateMode.RelativeX"/>.
	/// <see cref="double"/> otherwise.
	/// </summary>
	IComparable Y2 { get; set; }

	/// <summary>Brush to draw lines and borders.</summary>
	Brush Stroke { get; set; }

	/// <summary>Brush to fill background.</summary>
	Brush Fill { get; set; }

	/// <summary>Text color.</summary>
	Brush Foreground { get; set; }

	/// <summary>Line thickness.</summary>
	Thickness? Thickness { get; set; }

	/// <summary>Turn on/off label show for horizontal and vertical lines.</summary>
	bool? ShowLabel { get; set; }

	/// <summary>Label placement for horizontal and vertical lines.</summary>
	LabelPlacement? LabelPlacement { get; set; }

	/// <summary>Alignment for horizontal lines.</summary>
	HorizontalAlignment? HorizontalAlignment { get; set; }

	/// <summary>Alignment for vertical lines.</summary>
	VerticalAlignment? VerticalAlignment { get; set; }

	/// <summary>
	/// Coordinate mode.
	/// <see cref="AnnotationCoordinateMode.Absolute"/> means <see cref="DateTimeOffset"/> for X and <see cref="decimal"/> price for Y.
	/// <see cref="AnnotationCoordinateMode.Relative"/> means relative to the screen edges: double. 0=top/left, 0.5=center, 1=bottom/right
	/// </summary>
	AnnotationCoordinateMode? CoordinateMode { get; set; }

	/// <summary>Text for text annotation.</summary>
	string Text { get; set; }
}