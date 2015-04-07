namespace StockSharp.Sterling
{
	using System.ComponentModel;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

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
		}
	}
}