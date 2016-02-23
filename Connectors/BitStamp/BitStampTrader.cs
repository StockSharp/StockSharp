#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.BitStamp
File: BitStampTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp
{
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to the BitStamp.
	/// </summary>
	[Icon("BitStamp_logo.png")]
	public class BitStampTrader : Connector
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="BitStampTrader"/>.
		/// </summary>
		public BitStampTrader()
		{
			Adapter.InnerAdapters.Add(new BitStampMessageAdapter(TransactionIdGenerator));
		}

		private BitStampMessageAdapter NativeAdapter => Adapter.InnerAdapters.OfType<BitStampMessageAdapter>().First();

		/// <summary>
		/// Gets a value indicating whether the re-registration orders via the method <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// as a single transaction. The default is enabled.
		/// </summary>
		public override bool IsSupportAtomicReRegister => false;

		/// <summary>
		/// Key.
		/// </summary>
		public string Key
		{
			get { return NativeAdapter.Key.To<string>(); }
			set { NativeAdapter.Key = value.To<SecureString>(); }
		}

		/// <summary>
		/// Secret.
		/// </summary>
		public string Secret
		{
			get { return NativeAdapter.Secret.To<string>(); }
			set { NativeAdapter.Secret = value.To<SecureString>(); }
		}

		/// <summary>
		/// Client ID.
		/// </summary>
		public int ClientId
		{
			get { return NativeAdapter.ClientId; }
			set { NativeAdapter.ClientId = value; }
		}
    }
}