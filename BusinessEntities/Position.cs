namespace StockSharp.BusinessEntities
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Позиция по инструменту.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
	[DescriptionLoc(LocalizedStrings.Str541Key)]
	[CategoryOrderLoc(MainCategoryAttribute.NameKey, 0)]
	[CategoryOrderLoc(StatisticsCategoryAttribute.NameKey, 1)]
	public class Position : BasePosition
	{
		/// <summary>
		/// Создать <see cref="Position"/>.
		/// </summary>
		public Position()
		{
		}

		/// <summary>
		/// Портфель, в котором создана позиция.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str270Key)]
		[MainCategory]
		public Portfolio Portfolio { get; set; }

		/// <summary>
		/// Инструмент, по которому создана позиция.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str271Key)]
		[MainCategory]
		public Security Security { get; set; }

		/// <summary>
		/// Название депозитария, где находится физически ценная бумага.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.DepoKey)]
		[DescriptionLoc(LocalizedStrings.DepoNameKey)]
		[MainCategory]
		public string DepoName { get; set; }

		/// <summary>
		/// Вид лимита для Т+ рынка.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str272Key)]
		[DescriptionLoc(LocalizedStrings.Str267Key)]
		[MainCategory]
		[Nullable]
		public TPlusLimits? LimitType { get; set; }

		/// <summary>
		/// Создать копию объекта <see cref="Position"/>.
		/// </summary>
		/// <returns>Копия объекта.</returns>
		public Position Clone()
		{
			var clone = new Position();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Скопировать поля текущей позиции в <paramref name="destination"/>.
		/// </summary>
		/// <param name="destination">Позиция, в которую необходимо скопировать поля.</param>
		public void CopyTo(Position destination)
		{
			base.CopyTo(destination);

			destination.Portfolio = Portfolio;
			destination.Security = Security;
			destination.DepoName = DepoName;
			destination.LimitType = LimitType;
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0}-{1}".Put(Portfolio, Security);
		}
	}
}
