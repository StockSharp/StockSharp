namespace StockSharp.IQFeed
{
	using System.Net;

	/// <summary>
	/// Адреса IQ Connect.
	/// </summary>
	public static class IQFeedAddresses
	{
		/// <summary>
		/// Адрес по-умолчанию для получения служебных данных.
		/// </summary>
		public static readonly EndPoint DefaultAdminAddress = new IPEndPoint(IPAddress.Loopback, 9300);

		/// <summary>
		/// Адрес по-умолчанию для получения исторических данных.
		/// </summary>
		public static readonly EndPoint DefaultLookupAddress = new IPEndPoint(IPAddress.Loopback, 9100);

		/// <summary>
		/// Адрес по-умолчанию для получения данных по Level2.
		/// </summary>
		public static readonly EndPoint DefaultLevel2Address = new IPEndPoint(IPAddress.Loopback, 9200);

		/// <summary>
		/// Адрес по-умолчанию для получения данных по Level1.
		/// </summary>
		public static readonly EndPoint DefaultLevel1Address = new IPEndPoint(IPAddress.Loopback, 5009);

		/// <summary>
		/// Адрес по-умолчанию для получения производных данных.
		/// </summary>
		public static readonly EndPoint DefaultDerivativeAddress = new IPEndPoint(IPAddress.Loopback, 9400);
	}
}