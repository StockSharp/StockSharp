namespace StockSharp.Fix.Native;

using StockSharp.Messages;

/// <summary>
/// Data for DataTypeInfo FIX message.
/// </summary>
/// <param name="MdReqId">Market data request identifier.</param>
/// <param name="Message">Data type info message.</param>
public record struct FixDataTypeInfo(
	string MdReqId,
	DataTypeInfoMessage Message);
