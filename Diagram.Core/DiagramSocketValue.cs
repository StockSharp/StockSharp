namespace StockSharp.Diagram;

/// <summary>
/// The value for the connection.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DiagramSocketValue"/>.
/// </remarks>
/// <param name="socket">Connection.</param>
/// <param name="time">Time.</param>
/// <param name="value">Value.</param>
/// <param name="source">The source value.</param>
/// <param name="subscription">Subscription.</param>
public class DiagramSocketValue(DiagramSocket socket, DateTimeOffset time, object value, DiagramSocketValue source, Subscription subscription)
{
	/// <summary>
	/// The source value.
	/// </summary>
	public DiagramSocketValue Source { get; } = source;

	/// <summary>
	/// Connection.
	/// </summary>
	public DiagramSocket Socket { get; } = socket ?? throw new ArgumentNullException(nameof(socket));

	/// <summary>
	/// Time.
	/// </summary>
	public DateTimeOffset Time { get; } = time;

	/// <summary>
	/// Subscription.
	/// </summary>
	public Subscription Subscription { get; } = subscription;

	/// <summary>
	/// Value.
	/// </summary>
	public object Value { get; } = value;

	/// <summary>
	/// To get the value for the connection.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <returns>Value.</returns>
	public T GetValue<T>(bool canNull = false)
		=> (T)Value.ConvertValue(typeof(T), canNull);

	/// <inheritdoc />
	public override string ToString()
	{
		return Source is not null
			? "({0} -> {1}): {2}".Put(Source.Socket, Socket, Value)
			: "{0}: {1}".Put(Socket, Value);
	}
}