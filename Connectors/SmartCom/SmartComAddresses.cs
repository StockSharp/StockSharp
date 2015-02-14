namespace StockSharp.SmartCom
{
	using System.Net;

	using Ecng.Common;

	/// <summary>
	/// Адреса серверов системы SmartCOM. Описание доступно по адресу http://www.itinvest.ru/software/trade-servers/ .
	/// </summary>
	public static class SmartComAddresses
	{
		///// <summary>
		///// Порт сервера по умолчанию, равный 8090.
		///// </summary>
		//public const int DefaultPort = 8090;

		///// <summary>
		///// Основной сервер. IP адрес 82.204.220.34, порт 8090.
		///// </summary>
		//public static readonly EndPoint Major = "82.204.220.34:8090".To<EndPoint>();

		///// <summary>
		///// Вспомагательный сервер. IP адрес 213.247.232.238, порт 8090.
		///// </summary>
		//public static readonly EndPoint Minor = "213.247.232.238:8090".To<EndPoint>();

		///// <summary>
		///// Резервный сервер. IP адрес 87.118.223.109, порт 8090.
		///// </summary>
		//public static readonly EndPoint Reserv = "87.118.223.109:8090".To<EndPoint>();

		///// <summary>
		///// Cервер сталкера. IP адрес 89.175.35.230, порт 8090.
		///// </summary>
		//public static readonly EndPoint Stalker = "89.175.35.230:8090".To<EndPoint>();

		/// <summary>
		/// Демо сервер. IP адрес mxdemo.ittrade.ru, порт 8443.
		/// </summary>
		public static readonly EndPoint Demo = "mxdemo.ittrade.ru:8443".To<EndPoint>();

		/// <summary>
		/// MatriX™ сервер. IP адрес mx.ittrade.ru, порт 8443.
		/// </summary>
		public static readonly EndPoint Matrix = "mx.ittrade.ru:8443".To<EndPoint>();

		/// <summary>
		/// Резервный сервер. IP адрес st1.ittrade.ru, порт 8090.
		/// </summary>
		public static readonly EndPoint Reserve1 = "st1.ittrade.ru:8090".To<EndPoint>();

		/// <summary>
		/// Резервный сервер. IP адрес st2.ittrade.ru, порт 8090.
		/// </summary>
		public static readonly EndPoint Reserve2 = "st2.ittrade.ru:8090".To<EndPoint>();
	}
}