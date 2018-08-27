namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// The interface for provider of <see cref="IBasketSecurityProcessor"/>.
	/// </summary>
	public interface IBasketSecurityProcessorProvider
	{
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
		void UnRegister(string basketCode);

		/// <summary>
		/// Get processor type.
		/// </summary>
		/// <param name="basketCode">Basket security type.</param>
		/// <returns>Processor type.</returns>
		Type GetProcessorType(string basketCode);

		/// <summary>
		/// Get security type.
		/// </summary>
		/// <param name="basketCode">Basket security type.</param>
		/// <returns>Security type.</returns>
		Type GetSecurityType(string basketCode);
	}
}