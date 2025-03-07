namespace StockSharp.Messages;

/// <summary>
/// Message to request supported data types.
/// </summary>
[DataContract]
[Serializable]
public class DataTypeLookupMessage : BaseRequestMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DataTypeLookupMessage"/>.
	/// </summary>
	public DataTypeLookupMessage()
		: base(MessageTypes.DataTypeLookup)
	{
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.DataTypes;

	/// <summary>
	/// Create a copy of <see cref="DataTypeLookupMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return CopyTo(new DataTypeLookupMessage());
	}
}