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
	/// The storage, generating data in the process of operation.
	/// </summary>
	/// <typeparam name="T">Data type.</typeparam>
	public sealed class InMemoryMarketDataStorage<T> : IMarketDataStorage<T>
		where T : Message
	{
		private readonly Func<DateTimeOffset, IEnumerable<T>> _getData;

		/// <summary>
		/// Ñîçäàòü <see cref="InMemoryMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="security">Èíñòðóìåíò, ñ êîòîðûì ðàáîòàåò âíåøíåå õðàíèëèùå.</param>
		/// <param name="arg">Äîïîëíèòåëüíûé àðãóìåíò, àññîöèèðîâàííûé ñ äàííûìè. Íàïðèìåð, <see cref="Candle.Arg"/>.</param>
		/// <param name="getData">Ìåòîä ãåíåðàöèè äàííûõ äëÿ óêàçàííîé äàòû.</param>
		/// <param name="dataType">Òèï äàííûõ.</param>
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
		/// Ñîçäàòü <see cref="InMemoryMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="security">Èíñòðóìåíò, ñ êîòîðûì ðàáîòàåò âíåøíåå õðàíèëèùå.</param>
		/// <param name="arg">Äîïîëíèòåëüíûé àðãóìåíò, àññîöèèðîâàííûé ñ äàííûìè. Íàïðèìåð, <see cref="Candle.Arg"/>.</param>
		/// <param name="getData">Ìåòîä ãåíåðàöèè äàííûõ äëÿ óêàçàííîé äàòû.</param>
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
		/// To load data.
		/// </summary>
		/// <param name="date">Date, for which data shall be loaded.</param>
		/// <returns>Data. If there is no data, the empty set will be returned.</returns>
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