#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: PortfolioChangeMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Messages containing changes to the position.
	/// </summary>
	[DataContract]
	[Serializable]
	[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
	[DescriptionLoc(LocalizedStrings.Str541Key)]
	public sealed class PortfolioChangeMessage : PositionChangeMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioChangeMessage"/>.
		/// </summary>
		public PortfolioChangeMessage()
			//: base(MessageTypes.PortfolioChange)
		{
			SecurityId = SecurityId.Money;
		}

		/// <summary>
		/// Create a copy of <see cref="PortfolioChangeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new PortfolioChangeMessage();
			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",P={PortfolioName},CL={ClientCode},Changes={Changes.Select(c => c.ToString()).Join(",")}";
		}
	}
}