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
				{ "SecurityCode", value.SecurityId.SecurityCode },
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