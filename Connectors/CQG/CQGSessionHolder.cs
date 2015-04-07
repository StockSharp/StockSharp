namespace StockSharp.CQG
{
	using System.ComponentModel;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

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

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return string.Empty;
		}
	}
}