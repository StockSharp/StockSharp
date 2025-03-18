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

	/// <inheritdoc />
	[DataMember]
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// Possible data type.
	/// </summary>
	[DataMember]
	public DataType FileDataType { get; set; }

	private DateTime[] _dates = [];

	/// <summary>
	/// Dates.
	/// </summary>
	[DataMember]
	public DateTime[] Dates
	{
		get => _dates;
		set => _dates = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Storage format.
	/// </summary>
	[DataMember]
	public int Format { get; set; }

	/// <inheritdoc />
	public override DataType DataType => DataType.DataTypeInfo;

	/// <inheritdoc />
	public override void CopyTo(DataTypeInfoMessage destination)
	{
		base.CopyTo(destination);

		destination.SecurityId = SecurityId;
		destination.FileDataType = FileDataType?.TypedClone();
		destination.Dates = Dates?.ToArray();
		destination.Format = Format;
	}

	/// <inheritdoc />
	public override string ToString()
		=> base.ToString() + $",SecId={SecurityId},DT={FileDataType},DatesLen={Dates.Length},Fmt={Format}";
}