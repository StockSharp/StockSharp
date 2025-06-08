namespace StockSharp.Diagram;

/// <summary>
/// Link.
/// </summary>
public interface ICompositionModelLink : ICloneable
{
	/// <summary>
	/// Is reconnecting.
	/// </summary>
	bool IsReconnecting { get; set; }

	/// <summary>
	/// Is connected.
	/// </summary>
	bool IsConnected { get; set; }

	/// <summary>
	/// From node key.
	/// </summary>
	string From { get; set; }

	/// <summary>
	/// To node key.
	/// </summary>
	string To { get; set; }

	/// <summary>
	/// To socket key.
	/// </summary>
	string ToPort { get; set; }

	/// <summary>
	/// From socket key.
	/// </summary>
	string FromPort { get; set; }
}

/// <summary>
/// Dummy implementation of <see cref="ICompositionModelLink"/>.
/// </summary>
public class DummyCompositionModelLink : ICompositionModelLink
{
	bool ICompositionModelLink.IsReconnecting { get; set; }
	bool ICompositionModelLink.IsConnected { get; set; }

	/// <inheritdoc/>
	public string From { get; set; }
	/// <inheritdoc/>
	public string To { get; set; }
	/// <inheritdoc/>
	public string ToPort { get; set; }
	/// <inheritdoc/>
	public string FromPort { get; set; }

	object ICloneable.Clone() => new DummyCompositionModelLink
	{
		From = From,
		To = To,
		FromPort = FromPort,
		ToPort = ToPort,
	};

	/// <inheritdoc/>
	public override string ToString() => $"{From}({FromPort}) -> {To}({ToPort})";
}