namespace StockSharp.AlfaDirect
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Коды ошибок.
	/// </summary>
	public enum AlfaExceptionCodes
	{
		/// <summary>
		/// Критическая ошибка клиента.
		/// </summary>
		CriticalClientError = 1,
		
		/// <summary>
		/// Ошибка клиента.
		/// </summary>
		ClientError = 2,
		
		/// <summary>
		/// Нет соединения.
		/// </summary>
		NotConnected = 3,
		
		/// <summary>
		/// Ошибка сервера.
		/// </summary>
		ServerError = 4,
		
		/// <summary>
		/// Тайм-аут.
		/// </summary>
		Timeout = 5,
		
		/// <summary>
		/// Предупреждение.
		/// </summary>
		Warning = 6,
	}

	/// <summary>
	/// Исключение, содержащее код и текст ошибки.
	/// </summary>
	public class AlfaException : ApplicationException
	{
		internal AlfaException(ADLite.tagStateCodes code, string message)
			: base(LocalizedStrings.Str1701Params.Put(code, message))
		{
			Code = (AlfaExceptionCodes)(int)code;
		}

		/// <summary>
		/// Код ошибки.
		/// </summary>
		public AlfaExceptionCodes Code { get; private set; }
	}
}