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
			: base(message + LocalizedStrings.Str3698 + code)
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
				throw new ArgumentNullException("message");

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