#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.CorePublic
File: SessionList.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core
{
	using Ecng.Common;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;

	public class SessionList : BaseStorageEntityList<Session>
	{
		public SessionList(IStorage storage)
			: base(storage)
		{
		}

		public Session Battle
		{
			get { return ReadById(1L); }
		}
	}

	public class StrategyInfoSessionList : SessionList
	{
		public StrategyInfoSessionList(IStorage storage, StrategyInfo info)
			: base(storage)
		{
			CountQuery = Query
				.Select("count(*)")
				.From(Schema)
				.Join("SessionStrategy")
				.On("Session.Id", "SessionStrategy.Session")
				.Where()
				.Equals("SessionStrategy.StrategyInfo", info.Id.To<string>());

			ReadAllQuery = Query
				.Select(Schema)
				.From(Schema)
				.Join("SessionStrategy")
				.On("Session.Id", "SessionStrategy.Session")
				.Where()
				.Equals("SessionStrategy.StrategyInfo", info.Id.To<string>());

			ReadByIdQuery = Query
				.Select(Schema)
				.From(Schema)
				.Join("SessionStrategy")
				.On("Session.Id", "SessionStrategy.Session")
				.Where()
				.Equals("SessionStrategy.StrategyInfo", info.Id.To<string>())
				.And()
				.Equals(Schema.Fields["Id"]);
		}
	}
}