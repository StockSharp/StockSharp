namespace StockSharp.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Messages containing changes to the position.
	/// </summary>
	[DataContract]
	[Serializable]
	public sealed class PortfolioChangeMessage : BaseChangeMessage<PositionChangeTypes>
	{
		/// <summary>
		/// Portfolio name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string BoardCode { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioChangeMessage"/>.
		/// </summary>
		public PortfolioChangeMessage()
			: base(MessageTypes.PortfolioChange)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="PortfolioChangeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var msg = new PortfolioChangeMessage
			{
				LocalTime = LocalTime,
				PortfolioName = PortfolioName,
				BoardCode = BoardCode,
				ServerTime = ServerTime
			};

			msg.Changes.AddRange(Changes);
			this.CopyExtensionInfo(msg);

			return msg;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",P={0},Changes={1}".Put(PortfolioName, Changes.Select(c => c.ToString()).Join(","));
		}
	}
}