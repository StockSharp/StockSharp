namespace StockSharp.Btce
{
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
		private readonly BtceMessageAdapter _adapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="BtceTrader"/>.
		/// </summary>
		public BtceTrader()
		{
			_adapter = new BtceMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter.ToChannel(this));
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
			get { return _adapter.Key.To<string>(); }
			set { _adapter.Key = value.To<SecureString>(); }
		}

		/// <summary>
		/// Secret.
		/// </summary>
		public string Secret
		{
			get { return _adapter.Secret.To<string>(); }
			set { _adapter.Secret = value.To<SecureString>(); }
		}
	}
}