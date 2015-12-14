#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Btce.Btce
File: BtceTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Btce
{
	using System.Linq;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to the BTC-e.
	/// </summary>
	[Icon("Btce_logo.png")]
	public class BtceTrader : Connector
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BtceTrader"/>.
		/// </summary>
		public BtceTrader()
		{
			Adapter.InnerAdapters.Add(new BtceMessageAdapter(TransactionIdGenerator));
		}

		private BtceMessageAdapter NativeAdapter
		{
			get { return Adapter.InnerAdapters.OfType<BtceMessageAdapter>().First(); }
		}

		/// <summary>
		/// Gets a value indicating whether the re-registration orders via the method <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// as a single transaction. The default is enabled.
		/// </summary>
		public override bool IsSupportAtomicReRegister
		{
			get { return false; }
		}

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
	}
}