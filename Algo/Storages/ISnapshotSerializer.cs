namespace StockSharp.Algo.Storages;

/// <summary>
/// The interface for serialize snapshots.
/// </summary>
/// <typeparam name="TKey">Type of key value.</typeparam>
/// <typeparam name="TMessage">Message type.</typeparam>
public interface ISnapshotSerializer<TKey, TMessage>
	where TMessage : Message
{
	/// <summary>
	/// Data type info.
	/// </summary>
	DataType DataType { get; }

	/// <summary>
	/// Version of data format.
	/// </summary>
	Version Version { get; }

	/// <summary>
	/// Name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Serialize the specified message to byte array.
	/// </summary>
	/// <param name="version">Version of data format.</param>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Byte array.</returns>
	ValueTask<byte[]> SerializeAsync(Version version, TMessage message, CancellationToken cancellationToken);

	/// <summary>
	/// Deserialize message from byte array.
	/// </summary>
	/// <param name="version">Version of data format.</param>
	/// <param name="buffer">Byte array.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Message.</returns>
	ValueTask<TMessage> DeserializeAsync(Version version, byte[] buffer, CancellationToken cancellationToken);

	/// <summary>
	/// Get key for the specified message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>Key.</returns>
	TKey GetKey(TMessage message);

	/// <summary>
	/// Update the specified message by new changes.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="changes">Changes.</param>
	void Update(TMessage message, TMessage changes);
}