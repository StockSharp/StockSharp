#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Transaq
File: ApiException.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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