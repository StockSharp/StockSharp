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
			yield return new ColumnDescription(nameof(NewsMessage.Id))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(32),
			};
			yield return new ColumnDescription(nameof(NewsMessage.ServerTime)) { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription(nameof(NewsMessage.LocalTime)) { DbType = typeof(DateTimeOffset) };
			yield return new ColumnDescription(nameof(SecurityId.SecurityCode))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(NewsMessage.BoardCode))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(NewsMessage.Headline))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(NewsMessage.Story))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(int.MaxValue)
			};
			yield return new ColumnDescription(nameof(NewsMessage.Source))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(256)
			};
			yield return new ColumnDescription(nameof(NewsMessage.Url))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(1024)
			};
			yield return new ColumnDescription(nameof(NewsMessage.Priority))
			{
				DbType = typeof(int?),
			};
			yield return new ColumnDescription(nameof(NewsMessage.Language))
			{
				DbType = typeof(string),
				ValueRestriction = new StringRestriction(8)
			};
			yield return new ColumnDescription(nameof(NewsMessage.ExpiryDate)) { DbType = typeof(DateTimeOffset?) };
			yield return new ColumnDescription(nameof(NewsMessage.SeqNum)) { DbType = typeof(long?) };
		}

		protected override IDictionary<string, object> ConvertToParameters(NewsMessage value)
		{
			var result = new Dictionary<string, object>
			{
				{ nameof(NewsMessage.Id), value.Id },
				{ nameof(NewsMessage.ServerTime), value.ServerTime },
				{ nameof(NewsMessage.LocalTime), value.LocalTime },
				{ nameof(SecurityId.SecurityCode), value.SecurityId?.SecurityCode },
				{ nameof(NewsMessage.BoardCode), value.BoardCode },
				{ nameof(NewsMessage.Headline), value.Headline },
				{ nameof(NewsMessage.Story), value.Story },
				{ nameof(NewsMessage.Source), value.Source },
				{ nameof(NewsMessage.Url), value.Url },
				{ nameof(NewsMessage.Priority), value.Priority.To<int?>() },
				{ nameof(NewsMessage.Language), value.Language },
				{ nameof(NewsMessage.ExpiryDate), value.ExpiryDate },
				{ nameof(NewsMessage.SeqNum), value.SeqNum.DefaultAsNull() },
			};
			return result;
		}
	}
}