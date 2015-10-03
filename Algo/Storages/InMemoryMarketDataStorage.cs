namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Хранилище, генерирующее данные в процессе работы.
	/// </summary>
	/// <typeparam name="T">Тип данных.</typeparam>
	public sealed class InMemoryMarketDataStorage<T> : IMarketDataStorage<T>
		where T : Message
	{
		private readonly Func<DateTimeOffset, IEnumerable<T>> _getData;

		/// <summary>
		/// Создать <see cref="InMemoryMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="security">Инструмент, с которым работает внешнее хранилище.</param>
		/// <param name="arg">Дополнительный аргумент, ассоциированный с данными. Например, <see cref="Candle.Arg"/>.</param>
		/// <param name="getData">Метод генерации данных для указанной даты.</param>
		/// <param name="dataType">Тип данных.</param>
		public InMemoryMarketDataStorage(Security security, object arg, Func<DateTimeOffset, IEnumerable<Message>> getData, Type dataType = null)
		{
			if (getData == null)
				throw new ArgumentNullException("getData");

			_security = security;
			_arg = arg;
			_getData = d => getData(d).Cast<T>();
			_dataType = dataType ?? typeof(T);
		}

		/// <summary>
		/// Создать <see cref="InMemoryMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="security">Инструмент, с которым работает внешнее хранилище.</param>
		/// <param name="arg">Дополнительный аргумент, ассоциированный с данными. Например, <see cref="Candle.Arg"/>.</param>
		/// <param name="getData">Метод генерации данных для указанной даты.</param>
		public InMemoryMarketDataStorage(Security security, object arg, Func<DateTimeOffset, IEnumerable<T>> getData)
		{
			if (getData == null)
				throw new ArgumentNullException("getData");

			_security = security;
			_arg = arg;
			_getData = getData;
		}

		IEnumerable<DateTime> IMarketDataStorage.Dates
		{
			get { throw new NotSupportedException(); }
		}

		private readonly Security _security;

		Security IMarketDataStorage.Security
		{
			get { return _security; }
		}

		private readonly object _arg;

		object IMarketDataStorage.Arg
		{
			get { return _arg; }
		}

		IMarketDataStorageDrive IMarketDataStorage.Drive
		{
			get { throw new NotSupportedException(); }
		}

		bool IMarketDataStorage.AppendOnlyNew { get; set; }

		private readonly Type _dataType = typeof(T);

		Type IMarketDataStorage.DataType
		{
			get { return _dataType; }
		}

		IMarketDataSerializer IMarketDataStorage.Serializer
		{
			get { return ((IMarketDataStorage<T>)this).Serializer; }
		}

		IMarketDataSerializer<T> IMarketDataStorage<T>.Serializer
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Загрузить данные.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо загрузить данные.</param>
		/// <returns>Данные. Если данных не существует, то будет возвращено пустое множество.</returns>
		public IEnumerableEx<T> Load(DateTime date)
		{
			return _getData(date).ToEx();
		}

		IEnumerable IMarketDataStorage.Load(DateTime date)
		{
			return Load(date);
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage.Save(IEnumerable data)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage.Delete(IEnumerable data)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage.Delete(DateTime date)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage<T>.Save(IEnumerable<T> data)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage<T>.Delete(IEnumerable<T> data)
		{
			throw new NotSupportedException();
		}
	}
}