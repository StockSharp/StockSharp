#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.OpenECry.OpenECry
File: OpenECryAddresses.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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