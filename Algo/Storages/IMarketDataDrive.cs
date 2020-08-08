#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: IMarketDataDrive.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing the storage, associated with <see cref="IMarketDataStorage"/>.
	/// </summary>
	public interface IMarketDataStorageDrive
	{
		/// <summary>
		/// The storage (database, file etc.).
		/// </summary>
		IMarketDataDrive Drive { get; }

		/// <summary>
		/// To get all the dates for which market data are recorded.
		/// </summary>
		IEnumerable<DateTime> Dates { get; }

		/// <summary>
		/// To delete cache-files, containing information on available time ranges.
		/// </summary>
		void ClearDatesCache();

		/// <summary>
		/// To remove market data on specified date from the storage.
		/// </summary>
		/// <param name="date">Date, for which all data shall be deleted.</param>
		void Delete(DateTime date);

		/// <summary>
		/// To save data in the format of StockSharp storage.
		/// </summary>
		/// <param name="date">The date, for which data shall be saved.</param>
		/// <param name="stream">Data in the format of StockSharp storage.</param>
		void SaveStream(DateTime date, Stream stream);

		/// <summary>
		/// To load data in the format of StockSharp storage.
		/// </summary>
		/// <param name="date">Date, for which data shall be loaded.</param>
		/// <returns>Data in the format of StockSharp storage. If no data exists, <see cref="Stream.Null"/> will be returned.</returns>
		Stream LoadStream(DateTime date);
	}

	/// <summary>
	/// The interface, describing the storage (database, file etc.).
	/// </summary>
	public interface IMarketDataDrive : IPersistable, IDisposable
	{
		/// <summary>
		/// Path to market data.
		/// </summary>
		string Path { get; }

		/// <summary>
		/// To get news storage.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>The news storage.</returns>
		IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataSerializer<NewsMessage> serializer);

		/// <summary>
		/// To get the storage for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>The storage for the instrument.</returns>
		ISecurityMarketDataDrive GetSecurityDrive(Security security);

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		IEnumerable<SecurityId> AvailableSecurities { get; }

		/// <summary>
		/// Get all available data types.
		/// </summary>
		/// <param name="securityId">Instrument identifier.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Data types.</returns>
		IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format);

		/// <summary>
		/// To get the storage for <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Storage for <see cref="IMarketDataStorage"/>.</returns>
		IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format);

		/// <summary>
		/// Verify settings.
		/// </summary>
		void Verify();

		/// <summary>
		/// Download securities by the specified criteria.
		/// </summary>
		/// <param name="criteria">Message security lookup for specified criteria.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="newSecurity">The handler through which a new instrument will be passed.</param>
		/// <param name="isCancelled">The handler which returns an attribute of search cancel.</param>
		/// <param name="updateProgress">The handler through which a progress change will be passed.</param>
		void LookupSecurities(SecurityLookupMessage criteria, ISecurityProvider securityProvider, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress);
	}

	/// <summary>
	/// The base implementation <see cref="IMarketDataDrive"/>.
	/// </summary>
	public abstract class BaseMarketDataDrive : Disposable, IMarketDataDrive
	{
		/// <summary>
		/// Initialize <see cref="BaseMarketDataDrive"/>.
		/// </summary>
		protected BaseMarketDataDrive()
		{
		}

		/// <inheritdoc />
		public abstract string Path { get; set; }

		/// <inheritdoc />
		public IMarketDataStorage<NewsMessage> GetNewsMessageStorage(IMarketDataSerializer<NewsMessage> serializer)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public ISecurityMarketDataDrive GetSecurityDrive(Security security)
		{
			return new SecurityMarketDataDrive(this, security);
		}

		/// <inheritdoc />
		public abstract IEnumerable<SecurityId> AvailableSecurities { get; }

		/// <inheritdoc />
		public abstract IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format);

		/// <inheritdoc />
		public abstract IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format);

		/// <inheritdoc />
		public abstract void Verify();

		/// <inheritdoc />
		public abstract void LookupSecurities(SecurityLookupMessage criteria, ISecurityProvider securityProvider, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress);

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Path = storage.GetValue<string>(nameof(Path));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Path), Path);
		}

		/// <inheritdoc />
		public override string ToString() => Path;
	}
}