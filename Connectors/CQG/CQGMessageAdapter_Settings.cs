namespace StockSharp.CQG
{
	using System.ComponentModel;

	using StockSharp.Localization;

	[DisplayName("CQG")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.CQGConnectorKey)]
	partial class CQGMessageAdapter
	{
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