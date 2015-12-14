#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.Native.QuikPublic
File: ApiException.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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