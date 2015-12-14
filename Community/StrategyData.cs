#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: StrategyData.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// The strategy data.
	/// </summary>
	[DataContract]
	public class StrategyData
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// The creation date.
		/// </summary>
		[DataMember]
		public DateTime CreationDate { get; set; }

		/// <summary>
		/// Name.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Strategy description.
		/// </summary>
		[DataMember]
		public string Description { get; set; }

		/// <summary>
		/// The identifier of a topic in the forum where the strategy is discussed.
		/// </summary>
		[DataMember]
		public int TopicId { get; set; }

		/// <summary>
		/// The purchase price.
		/// </summary>
		[DataMember]
		public decimal Price { get; set; }

		/// <summary>
		/// Source code (if the strategy is distributed in source code).
		/// </summary>
		[DataMember]
		public string SourceCode { get; set; }

		/// <summary>
		/// The compiled build (if the strategy is distributed as a finished build).
		/// </summary>
		[DataMember]
		public byte[] CompiledAssembly { get; set; }

		/// <summary>
		/// The author identifier.
		/// </summary>
		[DataMember]
		public long Author { get; set; }
	}
}