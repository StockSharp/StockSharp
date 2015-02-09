namespace StockSharp.BusinessEntities
{
	using System.Collections.Generic;

	/// <summary>
	/// Интерфейс для доступа к поставщику информации об инструментах.
	/// </summary>
	public interface ISecurityProvider
	{
		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		IEnumerable<Security> Lookup(Security criteria);

		/// <summary>
		/// Получить внутренний идентификатор торговой системы.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Внутренний идентификатор торговой системы.</returns>
		object GetNativeId(Security security);
	}
}