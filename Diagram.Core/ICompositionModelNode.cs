namespace StockSharp.Diagram;

using System.Drawing;

/// <summary>
/// Node.
/// </summary>
public interface ICompositionModelNode : ICloneable
{
	/// <summary>
	/// Key.
	/// </summary>
	string Key { get; set; }

	/// <summary>
	/// <see cref="DiagramElement"/>
	/// </summary>
	DiagramElement Element { get; set; }

	/// <summary>
	/// Location.
	/// </summary>
	PointF Location { get; set; }

	/// <summary>
	/// Type id.
	/// </summary>
	Guid TypeId { get; set; }

	/// <summary>
	/// Figure id.
	/// </summary>
	string Figure { get; set; }

	/// <summary>
	/// Custom text.
	/// </summary>
	string Text { get; set; }
}

/// <summary>
/// Dummy implementation of <see cref="ICompositionModelNode"/>.
/// </summary>
public class DummyCompositionModelNode : ICompositionModelNode
{
	/// <inheritdoc/>
	public string Key { get; set; }
	/// <inheritdoc/>
	public DiagramElement Element { get; set; }
	/// <inheritdoc/>
	public PointF Location { get; set; }
	/// <inheritdoc/>
	public Guid TypeId { get; set; }

	/// <inheritdoc/>
	public string Figure { get; set; }
	/// <inheritdoc/>
	public string Text { get; set; }

	object ICloneable.Clone()
	{
		if (Element is null)
			throw new InvalidOperationException(LocalizedStrings.ElementNotLoaded.Put(Text));

		return new DummyCompositionModelNode
		{
			Key = Key,
			Element = Element.Clone(),
			Location = Location,
			TypeId = TypeId,
			Figure = Figure,
			Text = Text,
		};
	}

	/// <inheritdoc/>
	public override string ToString() => Element?.ToString() ?? base.ToString();
}