namespace StockSharp.Community
{
	using System.Runtime.Serialization;

	/// <summary>
	/// Product scopes.
	/// </summary>
	[DataContract]
	public enum ProductScopes
	{
		/// <summary>
		/// Public.
		/// </summary>
		[EnumMember]
		Public,

		/// <summary>
		/// Restricted (for example, paid connector).
		/// </summary>
		[EnumMember]
		Restricted,

		/// <summary>
		/// Private (visible for author only and his colleguaes).
		/// </summary>
		[EnumMember]
		Private,
	}
}