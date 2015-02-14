namespace StockSharp.OpenECry
{
	using System.Net;

	using Ecng.Common;

	/// <summary>
	/// Адреса серверов системы OpenECry.
	/// </summary>
	public static class OpenECryAddresses
	{
		/// <summary>
		/// Порт сервера по умолчанию, равный 9200.
		/// </summary>
		public const int DefaultPort = 9200;

		/// <summary>
		/// Основной сервер. Адрес api.openecry.com, порт 9200.
		/// </summary>
		public static readonly EndPoint Api = "api.openecry.com:9200".To<EndPoint>();

		/// <summary>
		/// Демо сервер. Адрес sim.openecry.com, порт 9200.
		/// </summary>
		public static readonly EndPoint Sim = "sim.openecry.com:9200".To<EndPoint>();
	}
}