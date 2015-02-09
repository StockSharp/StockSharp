namespace StockSharp.Sterling
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;

	using SterlingLib;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("Sterling")]
	[CategoryLoc(LocalizedStrings.Str2119Key)]
	[DescriptionLoc(LocalizedStrings.SterlingConnectorKey)]
	public class SterlingSessionHolder : MessageSessionHolder
	{
		/// <summary>
		/// Создать <see cref="SterlingSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public SterlingSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			CreateAssociatedSecurity = true;
			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;
		}

		private STIEvents _session;

		internal STIEvents Session
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
			return new SterlingMessageAdapter(MessageAdapterTypes.Transaction, this);
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return new SterlingMessageAdapter(MessageAdapterTypes.MarketData, this);
		}
	}
}