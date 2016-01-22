#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Export.Database.Algo
File: NewsTable.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Export.Database
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Messages;

	class NewsTable : Table<NewsMessage>
	{
		public NewsTable()
			: base("News", CreateColumns())
		{
		}

		private static IEnumerable<ColumnDescription> CreateColumns()
		{
			yield return new ColumnDescription("No") { DbType = typeof(long) };
			yield return new ColumnDescription("ServerTime") { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription("LocalTime") { DbType = typeof(DateTime) };
			yield return new ColumnDescription("SecurityCode")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("BoardCode")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("Headline")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("Story")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(int.MaxValue)
			};
			yield return new ColumnDescription("Source")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription("Url")
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(1024)
			};
		}

		protected override IDictionary<string, object> ConvertToParameters(NewsMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ "Id", value.Id },
				{ "ServerTime", value.ServerTime },
				{ "LocalTime", value.LocalTime },
				{ "SecurityCode", value.SecurityId?.SecurityCode },
				{ "BoardCode", value.BoardCode },
				{ "Headline", value.Headline },
				{ "Story", value.Story },
				{ "Source", value.Source },
				{ "Url", value.Url.To<string>() },
			};
			return result;
		}
	}
}