namespace StockSharp.Diagram;

/// <summary>
/// Loading context.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CompositionLoadingContext"/>.
/// </remarks>
/// <param name="allowCode"><see cref="AllowCode"/></param>
public class CompositionLoadingContext(bool allowCode)
{
	/// <summary>
	/// Allow <see cref="DiagramElement.IsExternalCode"/> elements.
	/// </summary>
	public bool AllowCode { get; } = allowCode;
}