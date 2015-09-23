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
		/// Удалить инструмент.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		void Delete(Security security);

		/// <summary>
		/// Удалить инструменты по критерию.
		/// </summary>
		/// <param name="criteria">Критерий.</param>
		void DeleteBy(Security criteria);

		/// <summary>
		/// Получить идентификаторы сохраненных инструментов.
		/// </summary>
		/// <returns>Идентификаторы инструментов.</returns>
		IEnumerable<string> GetSecurityIds();
	}
}