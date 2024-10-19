namespace StockSharp.Algo.Storages.Remote;

/// <summary>
/// Remote security extended info storage.
/// </summary>
public interface ISecurityRemoteExtendedStorage
{
	/// <summary>
	/// Security identifier.
	/// </summary>
	SecurityId SecurityId { get; }

	/// <summary>
	/// Add extended info.
	/// </summary>
	/// <param name="fieldValues">Extended information.</param>
	void AddSecurityExtendedInfo(object[] fieldValues);

	/// <summary>
	/// Delete extended info.
	/// </summary>
	void DeleteSecurityExtendedInfo();
}