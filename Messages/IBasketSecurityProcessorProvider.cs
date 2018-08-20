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
		/// <param name="code">Security type code.</param>
		/// <param name="processorType">Processor type.</param>
		/// <param name="securityType">Security type.</param>
		void Register(string code, Type processorType, Type securityType);

		/// <summary>
		/// Remove old security type.
		/// </summary>
		/// <param name="code">Security type code.</param>
		void UnRegister(string code);

		/// <summary>
		/// Get processor type.
		/// </summary>
		/// <param name="expression">Basket security expression.</param>
		/// <returns>Processor type.</returns>
		Type GetProcessorType(string expression);

		/// <summary>
		/// Get security type.
		/// </summary>
		/// <param name="expression">Basket security expression.</param>
		/// <returns>Security type.</returns>
		Type GetSecurityType(string expression);
	}
}