namespace StockSharp.BusinessEntities
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;
	using StockSharp.Localization;

	/// <summary>
	/// Идентификаторы инструмента в других системах.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.IdentifiersKey)]
	[DescriptionLoc(LocalizedStrings.Str603Key)]
	public class SecurityExternalId : Cloneable<SecurityExternalId>
	{
		/// <summary>
		/// Создать <see cref="SecurityExternalId"/>.
		/// </summary>
		public SecurityExternalId()
		{
		}

		/// <summary>
		/// Идентификатор в формате SEDOL (Stock Exchange Daily Official List).
		/// </summary>
		[DataMember]
		[DisplayName("SEDOL")]
		[DescriptionLoc(LocalizedStrings.Str351Key)]
		public string Sedol { get; set; }

		/// <summary>
		/// Идентификатор в формате CUSIP (Committee on Uniform Securities Identification Procedures).
		/// </summary>
		[DataMember]
		[DisplayName("CUSIP")]
		[DescriptionLoc(LocalizedStrings.Str352Key)]
		public string Cusip { get; set; }

		/// <summary>
		/// Идентификатор в формате ISIN (International Securities Identification Number).
		/// </summary>
		[DataMember]
		[DisplayName("ISIN")]
		[DescriptionLoc(LocalizedStrings.Str353Key)]
		public string Isin { get; set; }

		/// <summary>
		/// Идентификатор в формате RIC (Reuters Instrument Code).
		/// </summary>
		[DataMember]
		[DisplayName("RIC")]
		[DescriptionLoc(LocalizedStrings.Str354Key)]
		public string Ric { get; set; }

		/// <summary>
		/// Идентификатор в формате Bloomberg.
		/// </summary>
		[DataMember]
		[DisplayName("Bloomberg")]
		[DescriptionLoc(LocalizedStrings.Str355Key)]
		public string Bloomberg { get; set; }

		/// <summary>
		/// Идентификатор в формате IQFeed.
		/// </summary>
		[DataMember]
		[DisplayName("IQFeed")]
		[DescriptionLoc(LocalizedStrings.Str356Key)]
		public string IQFeed { get; set; }

		/// <summary>
		/// Идентификатор в формате Interactive Brokers.
		/// </summary>
		[DataMember]
		[DisplayName("Interactive Brokers")]
		[DescriptionLoc(LocalizedStrings.Str357Key)]
		[Nullable]
		public int? InteractiveBrokers { get; set; }

		/// <summary>
		/// Идентификатор в формате Plaza.
		/// </summary>
		[DataMember]
		[DisplayName("Plaza")]
		[DescriptionLoc(LocalizedStrings.Str358Key)]
		public string Plaza { get; set; }

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			var str = string.Empty;

			if (!Bloomberg.IsEmpty())
				str += " Bloom {0}".Put(Bloomberg);

			if (!Cusip.IsEmpty())
				str += " CUSIP {0}".Put(Cusip);

			if (!IQFeed.IsEmpty())
				str += " IQFeed {0}".Put(IQFeed);

			if (!Isin.IsEmpty())
				str += " ISIN {0}".Put(Isin);

			if (!Ric.IsEmpty())
				str += " RIC {0}".Put(Ric);

			if (!Sedol.IsEmpty())
				str += " SEDOL {0}".Put(Sedol);

			if (InteractiveBrokers != null)
				str += " InteractiveBrokers {0}".Put(InteractiveBrokers);

			if (!Plaza.IsEmpty())
				str += " Plaza {0}".Put(Plaza);

			return str;
		}

		/// <summary>
		/// Создать копию объекта <see cref="SecurityExternalId"/>.
		/// </summary>
		/// <returns>Копия объекта.</returns>
		public override SecurityExternalId Clone()
		{
			return new SecurityExternalId
			{
				Bloomberg = Bloomberg,
				Cusip = Cusip,
				IQFeed = IQFeed,
				Isin = Isin,
				Ric = Ric,
				Sedol = Sedol,
				InteractiveBrokers = InteractiveBrokers
			};
		}
	}
}