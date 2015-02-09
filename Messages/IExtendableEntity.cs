namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// Интерфейс для всех бизнес-объектов, которые имеют свойство <see cref="ExtensionInfo"/> для хранения расширенной информации.
	/// </summary>
	public interface IExtendableEntity
	{
		/// <summary>
		/// Расширенная информация.
		/// </summary>
		/// <remarks>
		/// Необходима в случае хранения в программе дополнительной информации.
		/// </remarks>
		IDictionary<object, object> ExtensionInfo { get; set; }
	}

	/// <summary>
	/// Вспомогательный класс для работы с <see cref="IExtendableEntity.ExtensionInfo"/>.
	/// </summary>
	public static class ExtandableEntityHelper
	{
		/// <summary>
		/// Добавить значение в <see cref="IExtendableEntity.ExtensionInfo"/>.
		/// </summary>
		/// <param name="entity">Сущность.</param>
		/// <param name="key">Ключ.</param>
		/// <param name="value">Значение.</param>
		public static void AddValue(this IExtendableEntity entity, object key, object value)
		{
			entity.GetExtInfo(true)[key] = value;
		}

		/// <summary>
		/// Получить значение из <see cref="IExtendableEntity.ExtensionInfo"/>.
		/// </summary>
		/// <typeparam name="T">Тип значения.</typeparam>
		/// <param name="entity">Сущность.</param>
		/// <param name="key">Ключ.</param>
		/// <returns>Значение.</returns>
		public static T GetValue<T>(this IExtendableEntity entity, object key)
		{
			var info = entity.GetExtInfo(false);

			if (info == null)
				return default(T);

			return (T)(info.TryGetValue(key) ?? default(T));
		}

		private static IDictionary<object, object> GetExtInfo(this IExtendableEntity entity, bool createIfNotExist)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			var info = entity.ExtensionInfo;

			if (info == null && createIfNotExist)
			{
				info = new SynchronizedDictionary<object, object>();
				entity.ExtensionInfo = info;
			}

			return info;
		}
	}
}