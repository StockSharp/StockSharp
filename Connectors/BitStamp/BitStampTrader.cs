namespace StockSharp.BitStamp
{
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
		private readonly BitStampMessageAdapter _adapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="BitStampTrader"/>.
		/// </summary>
		public BitStampTrader()
		{
			_adapter = new BitStampMessageAdapter(TransactionIdGenerator);

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

		/// <summary>
		/// Client ID.
		/// </summary>
		public int ClientId
		{
			get { return _adapter.ClientId; }
			set { _adapter.ClientId = value; }
		}
    }
}