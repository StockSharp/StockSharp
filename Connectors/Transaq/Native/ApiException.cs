namespace StockSharp.Transaq.Native
{
	using System;

	/// <summary>
	/// Класс ошибки при работе с API Transaq.
	/// </summary>
	public class ApiException : ApplicationException
	{
		internal ApiException(string message)
			: base(message)
		{
		}
	}
}