namespace StockSharp.Algo.Storages
{
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Интерфейс для представления в виде списка торговых объектов, полученных из внешнего хранилища.
	/// </summary>
	/// <typeparam name="T">Тип торгового объекта (например, <see cref="Security"/> или <see cref="MyTrade"/>).</typeparam>
	public interface IStorageEntityList<T> : INotifyList<T>, ISynchronizedCollection<T>
	{
		/// <summary>
		/// Загрузить торговый объект по идентификатору.
		/// </summary>
		/// <param name="id">Идентификатор.</param>
		/// <returns>Торговый объект. Если по идентификатору объект не был найден, то будет возвращено null.</returns>
		T ReadById(object id);

		/// <summary>
		/// Сохранить торговый объект.
		/// </summary>
		/// <param name="entity">Торговый объект.</param>
		void Save(T entity);

		/// <summary>
		/// Отложенное действие.
		/// </summary>
		DelayAction DelayAction { get; set; }

		/// <summary>
		/// Загрузить последние созданные данные.
		/// </summary>
		/// <param name="count">Количество запрашиваемых данных.</param>
		/// <returns>Диапазон данных.</returns>
		IEnumerable<T> ReadLasts(int count);
	}
}