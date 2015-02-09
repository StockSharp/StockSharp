namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка инструментов, хранящихся во внешнем хранилище.
	/// </summary>
	public class SecurityList : BaseStorageEntityList<Security>, IStorageSecurityList
	{
		private readonly EntityRegistry _registry;
		private readonly DatabaseCommand _readAllByCodeAndType;
		private readonly DatabaseCommand _readAllByCodeAndTypeAndExpiryDate;
		private readonly DatabaseCommand _readAllByType;
		private readonly DatabaseCommand _readAllByBoardAndType;
		private readonly DatabaseCommand _readAllByTypeAndExpiryDate;
		private readonly DatabaseCommand _readSecurityIds;

		/// <summary>
		/// Создать <see cref="SecurityList"/>.
		/// </summary>
		/// <param name="registry">Хранилище торговых объектов.</param>
		public SecurityList(EntityRegistry registry)
			: base(registry.Storage)
		{
			_registry = registry;

			var database = Storage as Database;

			if (database == null)
				return;

			var readAllByCodeAndType = database.CommandType == CommandType.StoredProcedure
				? Query.Execute(Schema, SqlCommandTypes.ReadAll, string.Empty, "CodeAndType")
				: Query
					.Select(Schema)
					.From(Schema)
					.Where()
						.Like(Schema.Fields["Code"])
						.And()
						.OpenBracket()
							.IsParamNull(Schema.Fields["Type"])
							.Or()
							.Equals(Schema.Fields["Type"])
						.CloseBracket();

			_readAllByCodeAndType = database.GetCommand(readAllByCodeAndType, Schema, new FieldList(new[] { Schema.Fields["Code"], Schema.Fields["Type"] }), new FieldList());

			var readAllByCodeAndTypeAndExpiryDate = database.CommandType == CommandType.StoredProcedure
				? Query.Execute(Schema, SqlCommandTypes.ReadAll, string.Empty, "CodeAndTypeAndExpiryDate")
				: Query
					.Select(Schema)
					.From(Schema)
					.Where()
						.Like(Schema.Fields["Code"])
						.And()
						.OpenBracket()
							.IsParamNull(Schema.Fields["Type"])
							.Or()
							.Equals(Schema.Fields["Type"])
						.CloseBracket()
						.And()
						.OpenBracket()
							.IsNull(Schema.Fields["ExpiryDate"])
							.Or()
							.Equals(Schema.Fields["ExpiryDate"])
						.CloseBracket();

			_readAllByCodeAndTypeAndExpiryDate = database.GetCommand(readAllByCodeAndTypeAndExpiryDate, Schema, new FieldList(new[] { Schema.Fields["Code"], Schema.Fields["Type"], Schema.Fields["ExpiryDate"] }), new FieldList());

			if (database.CommandType == CommandType.Text)
			{
				var readSecurityIds = Query
						.Execute("SELECT group_concat(Id, ',') FROM Security");

				_readSecurityIds = database.GetCommand(readSecurityIds, null, new FieldList(), new FieldList());

				var readAllByBoardAndType = Query
					.Select(Schema)
					.From(Schema)
					.Where()
						.Equals(Schema.Fields["Board"])
						.And()
						.OpenBracket()
							.IsParamNull(Schema.Fields["Type"])
							.Or()
							.Equals(Schema.Fields["Type"])
						.CloseBracket();

				_readAllByBoardAndType = database.GetCommand(readAllByBoardAndType, Schema, new FieldList(new[] { Schema.Fields["Board"], Schema.Fields["Type"] }), new FieldList());

				var readAllByTypeAndExpiryDate = Query
					.Select(Schema)
					.From(Schema)
					.Where()
						.Equals(Schema.Fields["Type"])
						.And()
						.OpenBracket()
							.IsNull(Schema.Fields["ExpiryDate"])
							.Or()
							.Equals(Schema.Fields["ExpiryDate"])
						.CloseBracket();

				_readAllByTypeAndExpiryDate = database.GetCommand(readAllByTypeAndExpiryDate, Schema, new FieldList(new[] { Schema.Fields["Type"], Schema.Fields["ExpiryDate"] }), new FieldList());

				var readAllByType = Query
					.Select(Schema)
					.From(Schema)
					.Where()
					.Equals(Schema.Fields["Type"]);

				_readAllByType = database.GetCommand(readAllByType, Schema, new FieldList(new[] { Schema.Fields["Type"] }), new FieldList());
			}
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			if (!criteria.Id.IsEmpty())
			{
				var security = ReadById(criteria.Id);
				return security == null ? Enumerable.Empty<Security>() : new[] { security };
			}
			
			if (!criteria.Code.IsEmpty() && _readAllByCodeAndType != null)
			{
				return criteria.ExpiryDate == null 
					? ReadAllByCodeAndType(criteria) 
					: ReadAllByCodeAndTypeAndExpiryDate(criteria);
			}

			if (criteria.Board != null && _readAllByBoardAndType != null)
			{
				return ReadAllByBoardAndType(criteria);
			}

			if (criteria.Type != null && _readAllByTypeAndExpiryDate != null)
			{
				return criteria.ExpiryDate == null
					? ReadAllByType(criteria)
					: ReadAllByTypeAndExpiryDate(criteria);
			}
			
			return this.Filter(criteria);
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}

		private IEnumerable<Security> ReadAllByCodeAndType(Security criteria)
		{
			var fields = new[]
			{
				new SerializationItem(Schema.Fields["Code"], "%" + criteria.Code + "%"),
				new SerializationItem(Schema.Fields["Type"], criteria.Type)
			};

			return Database.ReadAll<Security>(_readAllByCodeAndType, new SerializationItemCollection(fields));
		}

		private IEnumerable<Security> ReadAllByCodeAndTypeAndExpiryDate(Security criteria)
		{
			if (criteria.ExpiryDate == null)
				throw new ArgumentNullException("criteria", "ExpiryDate == null");

			var fields = new[]
			{
				new SerializationItem(Schema.Fields["Code"], "%" + criteria.Code + "%"),
				new SerializationItem(Schema.Fields["Type"], criteria.Type),
				new SerializationItem(Schema.Fields["ExpiryDate"], criteria.ExpiryDate.Value)
			};

			return Database.ReadAll<Security>(_readAllByCodeAndTypeAndExpiryDate, new SerializationItemCollection(fields));
		}

		private IEnumerable<Security> ReadAllByBoardAndType(Security criteria)
		{
			var fields = new[]
			{
				new SerializationItem(Schema.Fields["Board"], criteria.Board.Code),
				new SerializationItem(Schema.Fields["Type"], criteria.Type)
			};

			return Database.ReadAll<Security>(_readAllByCodeAndType, new SerializationItemCollection(fields));
		}

		private IEnumerable<Security> ReadAllByTypeAndExpiryDate(Security criteria)
		{
			if (criteria.ExpiryDate == null)
				throw new ArgumentNullException("criteria", "ExpiryDate == null");

			var fields = new[]
			{
				new SerializationItem(Schema.Fields["Type"], criteria.Type),
				new SerializationItem(Schema.Fields["ExpiryDate"], criteria.ExpiryDate.Value)
			};

			return Database.ReadAll<Security>(_readAllByTypeAndExpiryDate, new SerializationItemCollection(fields));
		}

		private IEnumerable<Security> ReadAllByType(Security criteria)
		{
			var fields = new[]
			{
				new SerializationItem(Schema.Fields["Type"], criteria.Type)
			};

			return Database.ReadAll<Security>(_readAllByType, new SerializationItemCollection(fields));
		}

		/// <summary>
		/// Сохранить торговый объект.
		/// </summary>
		/// <param name="entity">Торговый объект.</param>
		public override void Save(Security entity)
		{
			_registry.ExchangeBoards.Save(entity.Board);

			base.Save(entity);
		}

		/// <summary>
		/// Получить идентификаторы сохраненных инструментов.
		/// </summary>
		/// <returns>Идентификаторы инструментов.</returns>
		public IEnumerable<string> GetSecurityIds()
		{
			if (_readSecurityIds == null)
				return this.Select(s => s.Id);

			var str = _readSecurityIds.ExecuteScalar<string>(new SerializationItemCollection());
			return str.SplitByComma(",", true);
		}

		/// <summary>
		/// Вызывается при добавлении элемента в хранилище.
		/// </summary>
		/// <param name="entity">Торговый объект.</param>
		protected override void OnAdd(Security entity)
		{
			_registry.ExchangeBoards.Save(entity.Board);

			base.OnAdd(entity);
		}
	}
}