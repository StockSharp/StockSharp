namespace StockSharp.Messages;

/// <summary>
/// Message to request supported data types.
/// </summary>
[DataContract]
[Serializable]
public class DataTypeLookupMessage : BaseRequestMessage, ISecurityIdMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DataTypeLookupMessage"/>.
	/// </summary>
	public DataTypeLookupMessage()
		: base(MessageTypes.DataTypeLookup)
	{
	}

	/// <inheritdoc />
	[DataMember]
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// Data type info.
	/// </summary>
	[DataMember]
	public DataType RequestDataType { get; set; }

	/// <summary>
	/// Format.
	/// </summary>
	[DataMember]
	public int? Format { get; set; }

	/// <summary>
	/// Include dates.
	/// </summary>
	[DataMember]
	public bool IncludeDates { get; set; }

	/// <inheritdoc />
	public override DataType DataType => DataType.DataTypeInfo;

	/// <summary>
	/// Create a copy of <see cref="DataTypeLookupMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return CopyTo(new DataTypeLookupMessage
		{
			SecurityId = SecurityId,
			Format = Format,
			RequestDataType = RequestDataType?.TypedClone(),
			IncludeDates = IncludeDates,
		});
	}
}