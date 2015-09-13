namespace StockSharp.IQFeed
{
	using System.Net;

	/// <summary>
	/// IQ Connect addresses.
	/// </summary>
	public static class IQFeedAddresses
	{
		/// <summary>
		/// The default address for service data.
		/// </summary>
		public static readonly EndPoint DefaultAdminAddress = new IPEndPoint(IPAddress.Loopback, 9300);

		/// <summary>
		/// The default address for historical data.
		/// </summary>
		public static readonly EndPoint DefaultLookupAddress = new IPEndPoint(IPAddress.Loopback, 9100);

		/// <summary>
		/// The default address for data by Level2.
		/// </summary>
		public static readonly EndPoint DefaultLevel2Address = new IPEndPoint(IPAddress.Loopback, 9200);

		/// <summary>
		/// The default address for data by Level1.
		/// </summary>
		public static readonly EndPoint DefaultLevel1Address = new IPEndPoint(IPAddress.Loopback, 5009);

		/// <summary>
		/// The default address for derivative data.
		/// </summary>
		public static readonly EndPoint DefaultDerivativeAddress = new IPEndPoint(IPAddress.Loopback, 9400);
	}
}