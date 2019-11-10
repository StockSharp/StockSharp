#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: InMemoryMarketDataStorage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

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
		/// Initializes a new instance of the <see cref="InMemoryMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="arg">The additional argument, associated with data. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="getData">Handler for retrieving in-memory data.</param>
		/// <param name="dataType">Data type.</param>
		public InMemoryMarketDataStorage(SecurityId securityId, object arg, Func<DateTimeOffset, IEnumerable<Message>> getData, Type dataType = null)
			: this(securityId, arg, d => getData(d).Cast<T>(), dataType)
		{
			if (getData == null)
				throw new ArgumentNullException(nameof(getData));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="arg">The additional argument, associated with data. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="getData">Handler for retrieving in-memory data.</param>
		/// <param name="dataType">Data type.</param>
		public InMemoryMarketDataStorage(SecurityId securityId, object arg, Func<DateTimeOffset, IEnumerable<T>> getData, Type dataType = null)
		{
			_securityId = securityId;
			_arg = arg;
			_getData = getData ?? throw new ArgumentNullException(nameof(getData));
			_dataType = dataType ?? typeof(T);
		}

		IEnumerable<DateTime> IMarketDataStorage.Dates => throw new NotSupportedException();

		private readonly SecurityId _securityId;

		SecurityId IMarketDataStorage.SecurityId => _securityId;

		private readonly object _arg;

		object IMarketDataStorage.Arg => _arg;

		IMarketDataStorageDrive IMarketDataStorage.Drive => throw new NotSupportedException();

		bool IMarketDataStorage.AppendOnlyNew { get; set; }

		private readonly Type _dataType;

		Type IMarketDataStorage.DataType => _dataType;

		IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<T>)this).Serializer;

		IMarketDataSerializer<T> IMarketDataStorage<T>.Serializer => throw new NotSupportedException();

		/// <summary>
		/// To load data.
		/// </summary>
		/// <param name="date">Date, for which data shall be loaded.</param>
		/// <returns>Data. If there is no data, the empty set will be returned.</returns>
		public IEnumerable<T> Load(DateTime date)
		{
			return _getData(date);
		}

		IEnumerable IMarketDataStorage.Load(DateTime date)
		{
			return Load(date);
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			throw new NotSupportedException();
		}

		int IMarketDataStorage.Save(IEnumerable data)
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

		int IMarketDataStorage<T>.Save(IEnumerable<T> data)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage<T>.Delete(IEnumerable<T> data)
		{
			throw new NotSupportedException();
		}
	}
}