namespace StockSharp.Studio.Core
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;

	public class SessionStrategy
	{
		public SessionStrategy()
		{
			Settings = new SettingsStorage();
			Statistics = new SettingsStorage();
		}

		[Identity]
		[Field("Id", ReadOnly = true)]
		public long Id { get; set; }

		[RelationSingle]
		public Session Session { get; set; }

		[Field("Strategy")]
		public string StrategyId { get; set; }

		public SessionType SessionType { get; set; }

		[Ignore]
		public StrategyContainer Strategy { get; set; }

		[RelationSingle]
		public StrategyInfo StrategyInfo { get; set; }

		/// <summary>
		/// Хранилище, содержащее настройки стратегии.
		/// </summary>
		public SettingsStorage Settings { get; set; }

		public SettingsStorage Statistics { get; set; }

		/// <summary>
		/// Позиции стратегии для сессии.
		/// </summary>
		[RelationMany(typeof(SessionStrategyPositionlist))]
		public SessionStrategyPositionlist Positions { get; protected set; }
	}

	public class SessionStrategyPosition
	{
		[Identity]
		[Field("RowId", ReadOnly = true)]
		public long RowId { get; set; }

		[RelationSingle]
		public SessionStrategy Strategy { get; set; }

		[InnerSchema]
		public Position Position { get; set; }
	}

	public class SessionStrategyPositionlist : BaseStorageEntityList<SessionStrategyPosition>
	{
		private readonly SessionStrategy _strategy;
		private readonly SynchronizedDictionary<Tuple<Security, Portfolio>, SessionStrategyPosition> _cachedPositions = new SynchronizedDictionary<Tuple<Security, Portfolio>, SessionStrategyPosition>();

		public SessionStrategyPositionlist(IStorage storage, SessionStrategy strategy)
			: base(storage)
		{
			_strategy = strategy;
			//AddFilter(Schema.Fields["Session"], session, () => session);

			CountQuery = Query
				.Select("count(*)")
				.From(Schema)
				.Where()
				.Equals("Strategy", strategy.Id.To<string>());

			ReadAllQuery = Query
				.Select(Schema)
				.From(Schema)
				.Where()
				.Equals("Strategy", strategy.Id.To<string>());

			ReadByIdQuery = Query
				.Select(Schema)
				.From(Schema)
				.Where()
				.Equals("Strategy", strategy.Id.To<string>())
				.And()
				.Equals(Schema.Fields["RowId"]);
		}

		protected override IEnumerable<SessionStrategyPosition> OnGetGroup(long startIndex, long count, Field orderBy, ListSortDirection direction)
		{
			var positions = base.OnGetGroup(startIndex, count, orderBy, direction);

			foreach (var position in positions)
				_cachedPositions.SafeAdd(Tuple.Create(position.Position.Security, position.Position.Portfolio), p => position);

			return positions;
		}

		public override SessionStrategyPosition ReadById(object id)
		{
			var position = base.ReadById(id);

			if (position != null)
				_cachedPositions.SafeAdd(Tuple.Create(position.Position.Security, position.Position.Portfolio), p => position);

			return position;
		}

		public void Save(Position position)
		{
			bool isNew;

			var sessionPosition = _cachedPositions.SafeAdd(Tuple.Create(position.Security, position.Portfolio), p => new SessionStrategyPosition
			{
				Strategy = _strategy,
				Position = position
			}, out isNew);

			if (isNew)
				Add(sessionPosition);
			else
				Update(sessionPosition);
		}
	}
}