namespace StockSharp.Algo.Storages.Remote
{
	using System;
	using System.IO;
	using System.ServiceModel;

	using StockSharp.Algo.Storages;
	using StockSharp.Community;
	using StockSharp.Messages;

	/// <summary>
	/// The interface describing the external market data storage access to which is organized through the WCF network connection (for more details see <see cref="System.ServiceModel"/>).
	/// </summary>
	[ServiceContract(Namespace = "https://stocksharp.com/hydraserver")]
	public interface IRemoteStorage : IAuthenticationService
	{
		/// <summary>
		/// To find instrument identifiers that match the filter <paramref name="criteria" />.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="criteria">Message security lookup for specified criteria.</param>
		/// <returns>Found IDs securities.</returns>
		[OperationContract]
		string[] LookupSecurityIds(Guid sessionId, SecurityLookupMessage criteria);

		/// <summary>
		/// To find exchange codes that match the filter <paramref name="criteria" />.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="criteria">Message boards lookup for specified criteria.</param>
		/// <returns>Found codes.</returns>
		[OperationContract]
		string[] LookupExchanges(Guid sessionId, BoardLookupMessage criteria);

		/// <summary>
		/// To find exchange board codes that match the filter <paramref name="criteria" />.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="criteria">Message boards lookup for specified criteria.</param>
		/// <returns>Found codes.</returns>
		[OperationContract]
		string[] LookupExchangeBoards(Guid sessionId, BoardLookupMessage criteria);

		/// <summary>
		/// Get securities.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="securityIds">IDs securities.</param>
		/// <returns>Securities.</returns>
		[OperationContract]
		SecurityMessage[] GetSecurities(Guid sessionId, string[] securityIds);

		/// <summary>
		/// Get exchanges.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="codes">Exchange codes.</param>
		/// <returns>Exchanges.</returns>
		[OperationContract]
		string[] GetExchanges(Guid sessionId, string[] codes);

		/// <summary>
		/// Get exchange boards.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="codes">Board codes.</param>
		/// <returns>Exchange boards.</returns>
		[OperationContract]
		BoardMessage[] GetExchangeBoards(Guid sessionId, string[] codes);

		/// <summary>
		/// Save securities.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="securities">Securities.</param>
		[OperationContract]
		void SaveSecurities(Guid sessionId, SecurityMessage[] securities);

		/// <summary>
		/// Save exchanges.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="exchanges">Exchanges.</param>
		[OperationContract]
		void SaveExchanges(Guid sessionId, string[] exchanges);

		/// <summary>
		/// Save exchange boards.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="boards">Exchange boards.</param>
		[OperationContract]
		void SaveExchangeBoards(Guid sessionId, BoardMessage[] boards);

		/// <summary>
		/// Delete securities.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="securityIds">IDs securities.</param>
		[OperationContract]
		void DeleteSecurities(Guid sessionId, string[] securityIds);

		/// <summary>
		/// Delete exchanges.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="codes">Exchange codes.</param>
		[OperationContract]
		void DeleteExchanges(Guid sessionId, string[] codes);

		/// <summary>
		/// Delete exchange boards.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="codes">Exchange board codes.</param>
		[OperationContract]
		void DeleteExchangeBoards(Guid sessionId, string[] codes);

		/// <summary>
		/// Get security extended storage names.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>Storage names.</returns>
		[OperationContract]
		string[] GetSecurityExtendedStorages(Guid sessionId);

		/// <summary>
		/// Get security extended fields (names and types).
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="storageName">Storage name.</param>
		/// <returns>Extended fields (names and types).</returns>
		[OperationContract]
		Tuple<string, string>[] GetSecurityExtendedFields(Guid sessionId, string storageName);

		/// <summary>
		/// Get security extended fields (names and types).
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="storageName">Storage name.</param>
		/// <returns>IDs securities.</returns>
		[OperationContract]
		string[] GetExtendedInfoSecurities(Guid sessionId, string storageName);

		/// <summary>
		/// Get security extended info.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="storageName">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <returns>Extended information.</returns>
		[OperationContract]
		string[] GetSecurityExtendedInfo(Guid sessionId, string storageName, string securityId);

		/// <summary>
		/// Get security extended info.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="storageName">Storage name.</param>
		/// <returns>Extended information.</returns>
		[OperationContract]
		Tuple<string, string[]>[] GetAllExtendedInfo(Guid sessionId, string storageName);

		/// <summary>
		/// Create extended info storage.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="storageName">Storage name.</param>
		/// <param name="fields">Extended fields (names and types).</param>
		[OperationContract]
		void CreateSecurityExtendedFields(Guid sessionId, string storageName, Tuple<string, string>[] fields);

		/// <summary>
		/// Delete extended info storage.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="storageName">Storage name.</param>
		[OperationContract]
		void DeleteSecurityExtendedFields(Guid sessionId, string storageName);

		/// <summary>
		/// Add extended info.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="storageName">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="fieldValues">Extended information.</param>
		[OperationContract]
		void AddSecurityExtendedInfo(Guid sessionId, string storageName, string securityId, string[] fieldValues);

		/// <summary>
		/// Delete extended info.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="storageName">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		[OperationContract]
		void DeleteSecurityExtendedInfo(Guid sessionId, string storageName, string securityId);

		/// <summary>
		/// Get users.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>Users.</returns>
		[OperationContract]
		Tuple<string, string[], UserPermissions>[] GetUsers(Guid sessionId);

		/// <summary>
		/// Save user info.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <param name="ipAddresses">IP address list.</param>
		/// <param name="permissions">Permissions.</param>
		[OperationContract]
		void SaveUser(Guid sessionId, string login, string password, string[] ipAddresses, UserPermissions permissions);

		/// <summary>
		/// Delete existing user.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="login">Login.</param>
		[OperationContract]
		void DeleteUser(Guid sessionId, string login);

		/// <summary>
		/// Restart server.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		[OperationContract]
		void Restart(Guid sessionId);

		/// <summary>
		/// Start downloading.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns><see langword="true"/>, if downloading was start, otherwise, <see langword="false"/>.</returns>
		[OperationContract]
		bool StartDownloading(Guid sessionId);

		/// <summary>
		/// Stop downloading.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		[OperationContract]
		void StopDownloading(Guid sessionId);

		/// <summary>
		/// To get all the dates for which market data are recorded.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">Format type.</param>
		/// <returns>The range of available dates.</returns>
		[OperationContract]
		DateTime[] GetDates(Guid sessionId, string securityId, string dataType, string arg, StorageFormats format);

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>Data types.</returns>
		[OperationContract]
		string[] GetAvailableSecurities(Guid sessionId);

		/// <summary>
		/// Get all available data types.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="securityId">Instrument identifier.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Data types.</returns>
		[OperationContract]
		Tuple<string, string>[] GetAvailableDataTypes(Guid sessionId, string securityId, StorageFormats format);

		/// <summary>
		/// Save market-data into StockSharp storage format.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="date">The date for which you need to save the market data.</param>
		/// <param name="format">Format type.</param>
		/// <param name="data">Market data in the StockSharp storage format.</param>
		[OperationContract]
		void Save(Guid sessionId, string securityId, string dataType, string arg, DateTime date, StorageFormats format, byte[] data);

		/// <summary>
		/// To remove market data on specified date from the storage.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="date">The date on which you need to remove market data.</param>
		/// <param name="format">Format type.</param>
		[OperationContract]
		void Delete(Guid sessionId, string securityId, string dataType, string arg, DateTime date, StorageFormats format);

		/// <summary>
		/// To download market data in the StockSharp storage format.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="date">The date on which you need to download market data.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Market data in the StockSharp storage format. If the data does not exist then <see cref="Stream.Null"/> will be returned.</returns>
		[OperationContract]
		Stream LoadStream(Guid sessionId, string securityId, string dataType, string arg, DateTime date, StorageFormats format);
	}
}