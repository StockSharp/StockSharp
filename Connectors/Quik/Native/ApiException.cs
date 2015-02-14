namespace StockSharp.Quik.Native
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Исключение, содержащее код и текст ошибки.
	/// </summary>
	public class ApiException : ApplicationException
	{
		internal ApiException(Codes code, string message)
			: base(LocalizedStrings.Str1701Params.Put(code, message))
		{
			Code = code;
		}

		/// <summary>
		/// Код ошибки.
		/// </summary>
		public Codes Code { get; private set; }
	}
}