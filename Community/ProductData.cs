namespace StockSharp.Community
{
	using System.Runtime.Serialization;

	/// <summary>
	/// Product.
	/// </summary>
	[DataContract]
	public class ProductData
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Name.
		/// </summary>
		[DataMember]
		public string Name { get; set; }
	}
}