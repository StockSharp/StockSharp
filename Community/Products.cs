namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Products.
	/// </summary>
	[DataContract]
	[Obsolete]
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

		/// <summary>
		/// S#.Shell.
		/// </summary>
		[EnumMember]
		Shell,

		/// <summary>
		/// S#.MatLab.
		/// </summary>
		[EnumMember]
		MatLab,

		/// <summary>
		/// S#.Ë×È.
		/// </summary>
		[EnumMember]
		Lci,

		/// <summary>
		/// S#.Updater.
		/// </summary>
		[EnumMember]
		Installer,
	}
}