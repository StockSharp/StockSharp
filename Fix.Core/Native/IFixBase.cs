namespace StockSharp.Fix.Native;

/// <summary>
/// The interface describing basic functionality for the FIX protocol.
/// </summary>
public interface IFixBase : IDisposable
{
	/// <summary>
	/// The stream.
	/// </summary>
	Stream Stream { get; }

	/// <summary>
	/// The number of bytes write.
	/// </summary>
	int BytesCount { get; set; }

	/// <summary>
	/// <see cref="CheckSum"/> disabled.
	/// </summary>
	bool CheckSumDisabled { get; set; }

	/// <summary>
	/// Check sum.
	/// </summary>
#pragma warning disable 3003
	uint CheckSum { get; set; }
#pragma warning restore 3003

	/// <summary>
	/// Gets a value indicating whether the log incoming data required.
	/// </summary>
	bool IsDump { get; set; }

	/// <summary>
	/// Text encoding.
	/// </summary>
	Encoding Encoding { get; }
}
