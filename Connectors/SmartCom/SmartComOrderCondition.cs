#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.SmartCom.SmartCom
File: SmartComOrderCondition.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.SmartCom
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Условие заявок, специфичных для <see cref="SmartCom"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, "SmartCOM")]
	public class SmartComOrderCondition : OrderCondition
	{
		/// <summary>
		/// Создать <see cref="SmartComOrderCondition"/>.
		/// </summary>
		public SmartComOrderCondition()
		{
		}

		/// <summary>
		/// Стоп-цена заявки.
		/// </summary>
		[DataMember]
		[CategoryLoc(LocalizedStrings.Str225Key)]
		[DisplayNameLoc(LocalizedStrings.Str1885Key)]
		[DescriptionLoc(LocalizedStrings.Str1886Key)]
		public decimal? StopPrice
		{
			get { return (decimal?)Parameters.TryGetValue("StopPrice"); }
			set { Parameters["StopPrice"] = value; }
		}
	}
}