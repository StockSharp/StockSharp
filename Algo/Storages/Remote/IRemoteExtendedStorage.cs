namespace StockSharp.Algo.Storages.Remote;

/// <summary>
/// Remote extended info storage.
/// </summary>
public interface IRemoteExtendedStorage
{
	/// <summary>
	/// Storage name.
	/// </summary>
	string StorageName { get; }

	/// <summary>
	/// Get all security identifiers.
	/// </summary>
	IEnumerable<SecurityId> Securities { get; }

	/// <summary>
	/// Get security extended fields (names and types).
	/// </summary>
	Tuple<string, Type>[] Fields { get; }

	/// <summary>
	/// Get remote security extended info storage.
	/// </summary>
	/// <param name="securityId">Security identifier.</param>
	/// <returns>Remote security extended info storage.</returns>
	ISecurityRemoteExtendedStorage GetSecurityStorage(SecurityId securityId);

	/// <summary>
	/// Get security extended info.
	/// </summary>
	/// <returns>Extended information.</returns>
	Tuple<SecurityId, object[]>[] GetAllExtendedInfo();

	/// <summary>
	/// Create extended info storage.
	/// </summary>
	/// <param name="fields">Extended fields (names and types).</param>
	void CreateSecurityExtendedFields(Tuple<string, Type>[] fields);
}