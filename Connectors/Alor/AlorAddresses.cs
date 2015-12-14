#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Alor.Alor
File: AlorAddresses.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Alor
{
	/// <summary>
	/// Адреса серверов системы Alor-Trade.
	/// </summary>
	public static class AlorAddresses
	{
		///// <summary>
		///// Порт сервера по умолчанию, равный 7800.
		///// </summary>
		//public const int DefaultPort = 7800;

		/// <summary>
		/// Демо сервер. IP адрес 213.181.12.15.
		/// </summary>
		public static readonly string Study = "213.181.12.15";

		/// <summary>
		/// Просмотровый сервер на Forts (фьючерсы).  адрес 212.5.162.212.
		/// </summary>
		public static readonly string RtsFuturesReadOnly = "212.5.162.212";

		/// <summary>
		/// Просмотровый сервер на Forts (опционы).  адрес 212.5.162.213.
		/// </summary>
		public static readonly string RtsOptionsReadOnly = "212.5.162.213";

		/// <summary>
		/// Просмотровый сервер на Rts Standard.  адрес 195.146.76.141.
		/// </summary>
		public static readonly string RtsStandardReadOnly = "195.146.76.141";

		/// <summary>
		/// Демо сервер «Срочный рынок FORTS».  адрес 213.181.16.52.
		/// </summary>
		public static readonly string FortsDemo = "213.181.16.52";

		/// <summary>
		/// Демо сервер «24/7». IP адрес train.alor.ru.
		/// </summary>
		public static readonly string H247Demo = "train.alor.ru";
	}
}