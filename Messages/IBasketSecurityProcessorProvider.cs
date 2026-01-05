namespace StockSharp.Messages;

/// <summary>
/// The interface for provider of <see cref="IBasketSecurityProcessor"/>.
/// </summary>
public interface IBasketSecurityProcessorProvider
{
	/// <summary>
	/// All registered basket codes.
	/// </summary>
	IEnumerable<string> AllCodes { get; }

	/// <summary>
	/// Register new security type.
	/// </summary>
	/// <param name="basketCode">Basket security type.</param>
	/// <param name="processorType">Processor type.</param>
	/// <param name="securityType">Security type.</param>
	void Register(string basketCode, Type processorType, Type securityType);

	/// <summary>
	/// Remove old security type.
	/// </summary>
	/// <param name="basketCode">Basket security type.</param>
	/// <returns><see langword="true"/> if the code was found and removed; otherwise, <see langword="false"/>.</returns>
	bool UnRegister(string basketCode);

	/// <summary>
	/// Try get processor type.
	/// </summary>
	/// <param name="basketCode">Basket security type.</param>
	/// <param name="processorType">Processor type.</param>
	/// <returns><see langword="true"/> if the processor type was found; otherwise, <see langword="false"/>.</returns>
	bool TryGetProcessorType(string basketCode, out Type processorType);

	/// <summary>
	/// Try get security type.
	/// </summary>
	/// <param name="basketCode">Basket security type.</param>
	/// <param name="securityType">Security type.</param>
	/// <returns><see langword="true"/> if the security type was found; otherwise, <see langword="false"/>.</returns>
	bool TryGetSecurityType(string basketCode, out Type securityType);
}