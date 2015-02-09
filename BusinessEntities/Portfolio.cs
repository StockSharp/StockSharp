namespace StockSharp.BusinessEntities
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Портфель, описывающий торговый счет и размер сгенерированной комиссии по нему.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
	[DescriptionLoc(LocalizedStrings.Str541Key)]
	[CategoryOrderLoc(MainCategoryAttribute.NameKey, 0)]
	[CategoryOrderLoc(StatisticsCategoryAttribute.NameKey, 1)]
	public class Portfolio : BasePosition
	{
		/// <summary>
		/// Создать <see cref="Portfolio"/>.
		/// </summary>
		public Portfolio()
		{
		}

		private string _name;

		/// <summary>
		/// Кодовое название портфеля.
		/// </summary>
		[DataMember]
		[Identity]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string Name
		{
			get { return _name; }
			set
			{
				if (_name == value)
					return;

				_name = value;
				NotifyChanged("Name");
			}
		}

		private decimal _leverage;

		/// <summary>
		/// Плечо маржи.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str542Key)]
		[DescriptionLoc(LocalizedStrings.Str543Key)]
		[MainCategory]
		public decimal Leverage
		{
			get { return _leverage; }
			set
			{
				if (_leverage == value)
					return;

				_leverage = value;
				NotifyChanged("Leverage");
			}
		}

		private CurrencyTypes? _currency;

		/// <summary>
		/// Валюта портфеля.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str250Key)]
		[DescriptionLoc(LocalizedStrings.Str251Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency
		{
			get { return _currency; }
			set
			{
				_currency = value;
				NotifyChanged("Currency");
			}
		}

		[field: NonSerialized]
		private IConnector _connector;

		/// <summary>
		/// Подключение к торговой системе, через который был загружен данный портфель.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public IConnector Connector
		{
			get { return _connector; }
			set { _connector = value; }
		}

		/// <summary>
		/// Биржевая площадка, для которой действует данный портфель.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.Str544Key)]
		[MainCategory]
		public ExchangeBoard Board { get; set; }

		private PortfolioStates? _state;

		/// <summary>
		/// Состояние портфеля.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str252Key)]
		[MainCategory]
		[Nullable]
		public PortfolioStates? State
		{
			get { return _state; }
			set
			{
				if (_state == value)
					return;

				_state = value;
				NotifyChanged("State");
			}
		}

		private static readonly Portfolio _anonymousPortfolio = new Portfolio { Name = LocalizedStrings.Str545 };

		/// <summary>
		/// Портфель, ассоциированный с заявками, полученные через лог заявок.
		/// </summary>
		public static Portfolio AnonymousPortfolio
		{
			get { return _anonymousPortfolio; }
		}

		/// <summary>
		/// Создать копию объекта <see cref="Portfolio"/>.
		/// </summary>
		/// <returns>Копия объекта.</returns>
		public Portfolio Clone()
		{
			var clone = new Portfolio();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Скопировать поля текущего портфеля в <paramref name="destination"/>.
		/// </summary>
		/// <param name="destination">Портфель, в который необходимо скопировать поля.</param>
		public void CopyTo(Portfolio destination)
		{
			base.CopyTo(destination);

			destination.Name = Name;
			destination.Board = Board;
			destination.Currency = Currency;
			destination.Leverage = Leverage;
			destination.Connector = Connector;
			destination.State = State;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}
