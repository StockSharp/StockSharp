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