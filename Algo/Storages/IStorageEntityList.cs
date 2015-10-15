namespace StockSharp.Algo.Storages
{
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for presentation in the form of list of trade objects, received from the external storage.
	/// </summary>
	/// <typeparam name="T">The type of the trading object (for example, <see cref="Security"/> or <see cref="MyTrade"/>).</typeparam>
	public interface IStorageEntityList<T> : INotifyList<T>, ISynchronizedCollection<T>
	{
		/// <summary>
		/// To load the trading object by identifier.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <returns>The trading object. If the object was not found by identifier, <see langword="null" /> will be returned.</returns>
		T ReadById(object id);

		/// <summary>
		/// To save the trading object.
		/// </summary>
		/// <param name="entity">The trading object.</param>
		void Save(T entity);

		/// <summary>
		/// The time delayed action.
		/// </summary>
		DelayAction DelayAction { get; set; }

		/// <summary>
		/// To load last created data.
		/// </summary>
		/// <param name="count">The amount of requested data.</param>
		/// <returns>The data range.</returns>
		IEnumerable<T> ReadLasts(int count);
	}
}