namespace StockSharp.Community
{
	using System.Runtime.Serialization;

	/// <summary>
	/// Products.
	/// </summary>
	[DataContract]
	public enum Products
	{
		/// <summary>
		/// S#.API.
		/// </summary>
		[EnumMember]
		Api,

		/// <summary>
		/// S#.Data.
		/// </summary>
		[EnumMember]
		Hydra,

		/// <summary>
		/// S#.Studio.
		/// </summary>
		[EnumMember]
		Studio,

		///// <summary>
		///// S#.StrategyRunner.
		///// </summary>
		//[EnumMember]
		//StrategyRunner,

		/// <summary>
		/// S#.Designer.
		/// </summary>
		[EnumMember]
		Designer,

		/// <summary>
		/// S#.Designer.
		/// </summary>
		[EnumMember]
		Terminal,

		/// <summary>
		/// S#.Server.
		/// </summary>
		[EnumMember]
		Server,
	}
}