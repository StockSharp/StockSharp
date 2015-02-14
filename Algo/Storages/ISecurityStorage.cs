namespace StockSharp.Algo.Storages
{
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Интерфейс для доступа к хранилищу информации об инструментах.
	/// </summary>
	public interface ISecurityStorage : ISecurityProvider
	{
		/// <summary>
		/// Сохранить инструмент.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		void Save(Security security);

		/// <summary>
		/// Получить идентификаторы сохраненных инструментов.
		/// </summary>
		/// <returns>Идентификаторы инструментов.</returns>
		IEnumerable<string> GetSecurityIds();
	}
}