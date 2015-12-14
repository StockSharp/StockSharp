#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: IDailyInfoSoap.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Data;
	using System.ServiceModel;

	/// <summary>
	/// The interface for access to currency service.
	/// </summary>
	[ServiceContract(Namespace = "http://web.cbr.ru/")]
	public interface IDailyInfoSoap
	{
		/// <summary>
		/// To get currency exchange rates for the specific date.
		/// </summary>
		/// <param name="date">Date of rates.</param>
		/// <returns>Currency exchange rates.</returns>
		[OperationContract(Action = "http://web.cbr.ru/GetCursOnDate", ReplyAction = "*")]
		[XmlSerializerFormat]
		DataSet GetCursOnDate([MessageParameter(Name = "On_date")]DateTime date);
	}
}