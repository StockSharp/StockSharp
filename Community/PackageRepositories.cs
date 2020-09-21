namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Package repositories.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum PackageRepositories
	{
		/// <summary>
		/// NuGet.org
		/// </summary>
		[EnumMember]
		NuGet,

		/// <summary>
		/// StockSharp.com
		/// </summary>
		[EnumMember]
		StockSharp,
	}
}