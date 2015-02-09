namespace StockSharp.CQG
{
	using System;
	using System.ComponentModel;

	using Ecng.Collections;
	using Ecng.Common;

	using global::CQG;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("CQG")]
	[CategoryLoc(LocalizedStrings.Str2119Key)]
	[DescriptionLoc(LocalizedStrings.CQGConnectorKey)]
	public class CQGSessionHolder : MessageSessionHolder
	{
		/// <summary>
		/// Создать <see cref="CQGSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public CQGSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			CreateAssociatedSecurity = true;
			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;
		}

		private CQGCEL _session;

		internal CQGCEL Session
		{
			get { return _session; }
			set
			{
				if (_session != null)
					UnInitialize.SafeInvoke();

				_session = value;

				if (_session != null)
					Initialize.SafeInvoke();
			}
		}

		internal event Action Initialize;
		internal event Action UnInitialize;

		internal readonly SynchronizedDictionary<string, CQGInstrument> Instruments = new SynchronizedDictionary<string, CQGInstrument>();

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return string.Empty;
		}

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return new CQGMessageAdapter(MessageAdapterTypes.Transaction, this);
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return new CQGMessageAdapter(MessageAdapterTypes.MarketData, this);
		}
	}
}