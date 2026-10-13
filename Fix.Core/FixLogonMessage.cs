namespace StockSharp.Fix;

using System.Runtime.Serialization;

/// <summary>
/// FIX logon response.
/// </summary>
[DataContract]
[Serializable]
public class FixLogonMessage : ConnectMessage
{
	/// <summary>
	/// Support licensing.
	/// </summary>
	[DataMember]
	public bool SupportLicensing { get; set; }

	/// <summary>
	/// Demo mode connection.
	/// </summary>
	[DataMember]
	public bool IsDemo { get; set; }

	/// <summary>
	/// License feature id.
	/// </summary>
	[DataMember]
	public string LicenseFeatureId { get; set; }

	/// <summary>
	/// License component timestamp.
	/// </summary>
	[DataMember]
	public DateTime? LicenseComponentTimestamp { get; set; }

	/// <inheritdoc />
	public override Message Clone()
	{
		var clone = new FixLogonMessage
		{
			ClientVersion = ClientVersion,
			SessionId = SessionId,
			Language = Language,
			SupportLicensing = SupportLicensing,
			IsDemo = IsDemo,
			LicenseFeatureId = LicenseFeatureId,
			LicenseComponentTimestamp = LicenseComponentTimestamp,
		};

		CopyTo(clone);
		return clone;
	}
}
