#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.CorePublic
File: SessionStrategyList.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;

	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	using StockSharp.Algo.Strategies;

	public class SessionStrategyList : SessionBaseList<SessionStrategy>
	{
		private readonly Field _infoField = Schema.Fields["StrategyInfo"];
		private readonly Field _strategyField = Schema.Fields["Strategy"];
		//private readonly Field _fromField = Schema.Fields["Session"].Type.GetSchema().Fields["StartTime"];
		//private readonly Field _toField = Schema.Fields["Session"].Type.GetSchema().Fields["EndTime"];

		private readonly DatabaseCommand _readAllByStrategyInfo;
		private readonly DatabaseCommand _readByStrategy;
		//private readonly DatabaseCommand _readBySessionPeriod;

		public SessionStrategyList(IStorage storage, Session session)
			: base(storage, session)
		{
			Recycle = false;

			var id = Session.RowId.ToString(CultureInfo.InvariantCulture);

			CountQuery = Query
				.Select("count(*)")
				.From(Schema)
				.Where()
				.Equals("Session", id);

			ReadAllQuery = Query
				.Select(Schema)
				.From(Schema)
				.Where()
				.Equals("Session", id);

			var readAllByStrategyInfoQuery = Query
				.Select(Schema)
				.From(Schema)
				.Where()
				.Equals("Session", id)
				.And()
				.Equals(_infoField);

			_readAllByStrategyInfo = ((Database)Storage).GetCommand(readAllByStrategyInfoQuery, Schema, new FieldList(_infoField), new FieldList());

			var readByStrategy = Query
				.Select(Schema)
				.From(Schema)
				.Where()
				.Equals("Session", id)
				.And()
				.Equals(_strategyField);

			_readByStrategy = ((Database)Storage).GetCommand(readByStrategy, Schema, new FieldList(_strategyField), new FieldList());

			//var readBySessionPeriod = Query
			//    .Select(Schema)
			//    .From(Schema)
			//    .Where()
			//    .Equals("Session", id)
			//    .And()
			//    .Equals(new FieldList(_fromField, _toField));

			//_readBySessionPeriod = ((Database)Storage).GetCommand(readBySessionPeriod, new FieldList(_fromField, _toField), new FieldList());
		}

		//public IEnumerable<SessionStrategy> ReadAllBySessionPeriod(DateTime from, DateTime to)
		//{
		//    return Database.ReadAll<SessionStrategy>(_readBySessionPeriod, new SerializationItemCollection(new[]
		//    {
		//        new SerializationItem(_fromField, from),
		//        new SerializationItem(_toField, to),
		//    }));
		//}

		public IEnumerable<SessionStrategy> ReadAllByStrategyInfo(StrategyInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			return Database.ReadAll<SessionStrategy>(_readAllByStrategyInfo, new SerializationItemCollection(new[] { new SerializationItem(_infoField, info.Id.ToString(CultureInfo.InvariantCulture)) }));
		}

		public SessionStrategy ReadByStrategy(Strategy strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			return ReadByStrategyId(strategy.Id);
		}

		public SessionStrategy ReadByStrategyId(Guid id)
		{
			return Database.Read<SessionStrategy>(_readByStrategy, new SerializationItemCollection(new[] { new SerializationItem(_strategyField, id.ToString()) }));
		}
	}
}