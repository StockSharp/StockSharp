#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.AlfaDirect.AlfaDirect
File: AlfaException.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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