#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alor.Alor
File: AlorException.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Alor
{
	using System;

	using Ecng.Common;

	using TEClientLib;

	using StockSharp.Localization;

	/// <summary>
	/// Ошибка системы Alor.
	/// </summary>
	public class AlorException : ApplicationException
	{
		internal AlorException(string message, SFE code)
			: base(message + " " + LocalizedStrings.Str3698 + " " + code)
		{
			Code = code.ToString();
		}

		/// <summary>
		/// Код ошибки.
		/// </summary>
		public string Code { get; private set; }
	}

	static class AlorExceptionHelper
	{
		public static AlorException GetException(int result, string message)
		{
			if (message.IsEmpty())
				throw new ArgumentNullException(nameof(message));

			var code = (SFE)result;

			return code != SFE.SFE_OK ? new AlorException(message, code) : null;
		}

		public static void ThrowIfNeed(this int result, string message)
		{
			var error = GetException(result, message);

			if (error != null)
				throw error;
		}
	}
}