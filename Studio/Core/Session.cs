namespace StockSharp.Studio.Core
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	public enum SessionType
	{
		[EnumDisplayNameLoc(LocalizedStrings.Str3176Key)]
		Battle,

		[EnumDisplayNameLoc(LocalizedStrings.Str1174Key)]
		Emulation,

		[EnumDisplayNameLoc(LocalizedStrings.Str3177Key)]
		Optimization,
	}

	public class Session : NotifiableObject
	{
		private DateTime _startTime;
		private DateTime _endTime;

		public Session()
		{
			Settings = new SettingsStorage();
		}

		[Identity]
		[Field("Id", ReadOnly = true)]
		public long RowId { get; set; }

		/// <summary>
		/// Тип сессии.
		/// </summary>
		public SessionType Type { get; set; }

		/// <summary>
		/// Время запуска сессии.
		/// </summary>
		public DateTime StartTime
		{
			get { return _startTime; }
			set
			{
				_startTime = value;
				NotifyChanged("StartTime");
			}
		}

		/// <summary>
		/// Время остановки сессии.
		/// </summary>
		public DateTime EndTime
		{
			get { return _endTime; }
			set
			{
				_endTime = value;
				NotifyChanged("EndTime");
			}
		}

		/// <summary>
		/// Хранилище, содержащее настройки для сессии (настройки эмуляции и т.д.).
		/// </summary>
		public SettingsStorage Settings { get; set; }

		[RelationMany(typeof(SessionStrategyList))]
		public SessionStrategyList Strategies { get; protected set; }

		[RelationMany(typeof(SessionNewsList))]
		public SessionNewsList News { get; protected set; }
	}

	public class SessionNewsList : SessionBaseList<News>
	{
		private sealed class SessionNews
		{
			[Identity]
			[Field("RowId", ReadOnly = true)]
			public long RowId { get; set; }

			[RelationSingle]
			public Session Session { get; set; }

			[InnerSchema]
			public News News { get; set; }
		}

		private sealed class SessionNewsInfoList : SessionBaseList<SessionNews>
		{
			private readonly DatabaseCommand _readByNewsId;
			//private readonly Field _sessionField = Schema.Fields["Session"];
			private readonly Field _newsIdField = Schema.Fields["News"].Type.GetSchema().Fields["Id"];

			public SessionNewsInfoList(IStorage storage, Session session)
				: base(storage, session)
			{
				CountQuery = Query
					.Select("count(*)")
					.From(Schema)
					.Where()
					.Equals("Session", Session.RowId.To<string>());

				ReadAllQuery = Query
					.Select(Schema)
					.From(Schema)
					.Where()
					.Equals("Session", Session.RowId.To<string>());

				var readByNewsId = Query
					.Select(Schema)
					.From(Schema)
					.Where()
					.Equals(_newsIdField)
					.And()
					.Equals("Session", Session.RowId.To<string>());

				_readByNewsId = ((Database)Storage).GetCommand(readByNewsId, Schema, new FieldList(_newsIdField), new FieldList());
			}

			public SessionNews ReadByNewsId(long id)
			{
				return Database.Read<SessionNews>(_readByNewsId, new SerializationItemCollection(new[] { new SerializationItem(_newsIdField, id.To<string>()) }));
			}
		}

		private readonly SessionNewsInfoList _newsList;

		public SessionNewsList(IStorage storage, Session session) 
			: base(storage, session)
		{
			_newsList = new SessionNewsInfoList(storage, session);
		}

		protected override long OnGetCount()
		{
			return _newsList.Count;
		}

		public override News ReadById(object id)
		{
			var news = _newsList.ReadByNewsId((long)id);

			return news == null ? null : news.News;
		}

		protected override IEnumerable<News> OnGetGroup(long startIndex, long count, Field orderBy, ListSortDirection direction)
		{
			return _newsList.ReadAll(startIndex, count, orderBy, direction).Select(n => n.News).ToArray();
		}

		public override void Add(News item)
		{
			_newsList.Add(new SessionNews { News = item, Session = Session });
		}

		//public override void Update(News entity)
		//{
		//	var news = _newsList.ReadByNewsId(entity.Id);
		//	news.News = entity;
		//	_newsList.Update(news);
		//}
	}
}