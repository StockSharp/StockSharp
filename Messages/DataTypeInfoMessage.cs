namespace StockSharp.Messages;

/// <summary>
/// Data types search result message.
/// </summary>
[DataContract]
[Serializable]
public class DataTypeInfoMessage : BaseSubscriptionIdMessage<DataTypeInfoMessage>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DataTypeInfoMessage"/>.
	/// </summary>
	public DataTypeInfoMessage()
		: base(MessageTypes.DataTypeInfo)
	{
	}

	private DataType[] _dataTypes = [];

	/// <summary>
	/// Possible data types.
	/// </summary>
	[DataMember]
	public DataType[] DataTypes
	{
		get => _dataTypes;
		set => _dataTypes = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.DataTypes;

	/// <inheritdoc />
	public override void CopyTo(DataTypeInfoMessage destination)
	{
		base.CopyTo(destination);

		destination.DataTypes = DataTypes;
	}

	/// <inheritdoc />
	public override string ToString()
		=> base.ToString() + $",DT={DataTypes.Select(t => t.Name).JoinCommaSpace()}";
}