namespace StockSharp.Transaq
{
	using System.Net;

	using Ecng.Common;

	/// <summary>
	/// Адреса серверов Transaq.
	/// </summary>
	public static class TransaqAddresses
	{
		/// <summary>
		/// Финам демо сервер. IP адрес 78.41.194.72, порт 3939.
		/// </summary>
		public static readonly EndPoint FinamDemo = "78.41.194.72:3939".To<EndPoint>();

		/// <summary>
		/// Финам боевой сервер 1. IP адрес 213.247.141.133, порт 3900.
		/// </summary>
		public static readonly EndPoint FinamReal1 = "213.247.141.133:3900".To<EndPoint>();

		/// <summary>
		/// Финам боевой сервер 2. IP адрес 78.41.199.24, порт 3900.
		/// </summary>
		public static readonly EndPoint FinamReal2 = "78.41.199.24:3900".To<EndPoint>();
	}
}