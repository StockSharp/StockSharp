namespace StockSharp.Diagram;

using System.Drawing;

/// <summary>
/// Node.
/// </summary>
public interface ICompositionModelNode
{
	/// <summary>
	/// Create a copy of the node together with its element.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Copy.</returns>
	ValueTask<ICompositionModelNode> CloneAsync(CancellationToken cancellationToken);

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
	/// Settings of the element the node stands for, while <see cref="Element"/> is not loaded: its type was
	/// not known when the composition was read, or its settings could not be read. A save writes them back.
	/// </summary>
	SettingsStorage ElementSettings { get; set; }

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
/// In-memory implementation of <see cref="ICompositionModelNode"/>.
/// </summary>
public class InMemoryCompositionModelNode : ICompositionModelNode
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
	public SettingsStorage ElementSettings { get; set; }

	/// <inheritdoc/>
	public string Figure { get; set; }
	/// <inheritdoc/>
	public string Text { get; set; }

	/// <inheritdoc />
	public async ValueTask<ICompositionModelNode> CloneAsync(CancellationToken cancellationToken)
	{
		if (Element is null)
			throw new InvalidOperationException(LocalizedStrings.ElementNotLoaded.Put(Text));

		return new InMemoryCompositionModelNode
		{
			Key = Key,
			Element = await Element.CloneAsync(true, cancellationToken),
			Location = Location,
			TypeId = TypeId,
			Figure = Figure,
			Text = Text,
		};
	}

	/// <inheritdoc/>
	public override string ToString() => Element?.ToString() ?? base.ToString();
}