namespace StockSharp.OpenECry
{
	using System.Net;

	using Ecng.Common;

	/// <summary>
	/// Addresses of OpenECry system servers.
	/// </summary>
	public static class OpenECryAddresses
	{
		/// <summary>
		/// The server port default value is 9200.
		/// </summary>
		public const int DefaultPort = 9200;

		/// <summary>
		/// The main server. Address is api.openecry.com, port 9200.
		/// </summary>
		public static readonly EndPoint Api = "api.openecry.com:9200".To<EndPoint>();

		/// <summary>
		/// Demo server. Address is sim.openecry.com, port 9200.
		/// </summary>
		public static readonly EndPoint Sim = "sim.openecry.com:9200".To<EndPoint>();
	}
}