#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: SecurityList.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Data;
	using Ecng.Data.Sql;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation in the form of list of instruments, stored in external storage.
	/// </summary>
	public class SecurityList : BaseStorageEntityList<Security>, IStorageSecurityList
	{
		private readonly IEntityRegistry _registry;

		private readonly DatabaseCommand _readAllByCodeAndType;
		private readonly DatabaseCommand _readAllByCodeAndTypeAndExpiryDate;
		private readonly DatabaseCommand _readAllByType;
		private readonly DatabaseCommand _readAllByBoardAndType;
		private readonly DatabaseCommand _readAllByTypeAndExpiryDate;
		private readonly DatabaseCommand _readSecurityIds;

		private const string _code = nameof(Security.Code);
		private const string _type = nameof(Security.Type);
		private const string _expiryDate = nameof(Security.ExpiryDate);
		private const string _board = nameof(Security.Board);
		private const string _id = nameof(Security.Id);

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityList"/>.
		/// </summary>
		/// <param name="registry">The storage of trade objects.</param>
		public SecurityList(IEntityRegistry registry)
			: base(registry.Storage)
		{
			_registry = registry;

			if (!(Storage is Database database))
				return;

			var readAllByCodeAndType = database.CommandType == CommandType.StoredProcedure
				? Query.Execute(Schema, SqlCommandTypes.ReadAll, string.Empty, "CodeAndType")
				: Query
					.Select(Schema)
					.From(Schema)
					.Where()
						.Like(Schema.Fields[_code])
						.And()
						.OpenBracket()
							.IsParamNull(Schema.Fields[_type])
							.Or()
							.Equals(Schema.Fields[_type])
						.CloseBracket();

			_readAllByCodeAndType = database.GetCommand(readAllByCodeAndType, Schema, new FieldList(Schema.Fields[_code], Schema.Fields[_type]), new FieldList());

			var readAllByCodeAndTypeAndExpiryDate = database.CommandType == CommandType.StoredProcedure
				? Query.Execute(Schema, SqlCommandTypes.ReadAll, string.Empty, "CodeAndTypeAndExpiryDate")
				: Query
					.Select(Schema)
					.From(Schema)
					.Where()
						.Like(Schema.Fields[_code])
						.And()
						.OpenBracket()
							.IsParamNull(Schema.Fields[_type])
							.Or()
							.Equals(Schema.Fields[_type])
						.CloseBracket()
						.And()
						.OpenBracket()
							.IsNull(Schema.Fields[_expiryDate])
							.Or()
							.Equals(Schema.Fields[_expiryDate])
						.CloseBracket();

			_readAllByCodeAndTypeAndExpiryDate = database.GetCommand(readAllByCodeAndTypeAndExpiryDate, Schema, new FieldList(Schema.Fields[_code], Schema.Fields[_type], Schema.Fields[_expiryDate]), new FieldList());

			if (database.CommandType == CommandType.Text)
			{
				var readSecurityIds = Query
						.Execute("SELECT group_concat(Id, ',') FROM Security");

				_readSecurityIds = database.GetCommand(readSecurityIds, null, new FieldList(), new FieldList());

				var readAllByBoardAndType = Query
					.Select(Schema)
					.From(Schema)
					.Where()
						.Equals(Schema.Fields[_board])
						.And()
						.OpenBracket()
							.IsParamNull(Schema.Fields[_type])
							.Or()
							.Equals(Schema.Fields[_type])
						.CloseBracket();

				_readAllByBoardAndType = database.GetCommand(readAllByBoardAndType, Schema, new FieldList(Schema.Fields[_board], Schema.Fields[_type]), new FieldList());

				var readAllByTypeAndExpiryDate = Query
					.Select(Schema)
					.From(Schema)
					.Where()
						.Equals(Schema.Fields[_type])
						.And()
						.OpenBracket()
							.IsNull(Schema.Fields[_expiryDate])
							.Or()
							.Equals(Schema.Fields[_expiryDate])
						.CloseBracket();

				_readAllByTypeAndExpiryDate = database.GetCommand(readAllByTypeAndExpiryDate, Schema, new FieldList(Schema.Fields[_type], Schema.Fields[_expiryDate]), new FieldList());

				var readAllByType = Query
					.Select(Schema)
					.From(Schema)
					.Where()
					.Equals(Schema.Fields[_type]);

				_readAllByType = database.GetCommand(readAllByType, Schema, new FieldList(Schema.Fields[_type]), new FieldList());

				RemoveQuery = Query
					.Delete()
					.From(Schema)
					.Where()
					.Equals(Schema.Fields[_id]);
			}

			((ICollectionEx<Security>)this).AddedRange += s => _added?.Invoke(s);
			((ICollectionEx<Security>)this).RemovedRange += s => _removed?.Invoke(s);
		}

		DelayAction IStorageEntityList<Security>.DelayAction => DelayAction;

		private Action<IEnumerable<Security>> _added;

		event Action<IEnumerable<Security>> ISecurityProvider.Added
		{
			add => _added += value;
			remove => _added -= value;
		}

		private Action<IEnumerable<Security>> _removed;

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add => _removed += value;
			remove => _removed -= value;
		}

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		public IEnumerable<Security> Lookup(Security criteria)
		{
			if (criteria.IsLookupAll())
				return this.ToArray();

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

		private IEnumerable<Security> ReadAllByCodeAndType(Security criteria)
		{
			var fields = new[]
			{
				new SerializationItem(Schema.Fields[_code], "%" + criteria.Code + "%"),
				new SerializationItem(Schema.Fields[_type], criteria.Type)
			};

			return Database.ReadAll<Security>(_readAllByCodeAndType, new SerializationItemCollection(fields));
		}

		private IEnumerable<Security> ReadAllByCodeAndTypeAndExpiryDate(Security criteria)
		{
			if (criteria.ExpiryDate == null)
				throw new ArgumentNullException(nameof(criteria), "ExpiryDate == null");

			var fields = new[]
			{
				new SerializationItem(Schema.Fields[_code], "%" + criteria.Code + "%"),
				new SerializationItem(Schema.Fields[_type], criteria.Type),
				new SerializationItem(Schema.Fields[_expiryDate], criteria.ExpiryDate.Value)
			};

			return Database.ReadAll<Security>(_readAllByCodeAndTypeAndExpiryDate, new SerializationItemCollection(fields));
		}

		private IEnumerable<Security> ReadAllByBoardAndType(Security criteria)
		{
			var fields = new[]
			{
				new SerializationItem(Schema.Fields[_board], criteria.Board.Code),
				new SerializationItem(Schema.Fields[_type], criteria.Type)
			};

			return Database.ReadAll<Security>(_readAllByCodeAndType, new SerializationItemCollection(fields));
		}

		private IEnumerable<Security> ReadAllByTypeAndExpiryDate(Security criteria)
		{
			if (criteria.ExpiryDate == null)
				throw new ArgumentNullException(nameof(criteria), "ExpiryDate == null");

			var fields = new[]
			{
				new SerializationItem(Schema.Fields[_type], criteria.Type),
				new SerializationItem(Schema.Fields[_expiryDate], criteria.ExpiryDate.Value)
			};

			return Database.ReadAll<Security>(_readAllByTypeAndExpiryDate, new SerializationItemCollection(fields));
		}

		private IEnumerable<Security> ReadAllByType(Security criteria)
		{
			var fields = new[]
			{
				new SerializationItem(Schema.Fields[_type], criteria.Type)
			};

			return Database.ReadAll<Security>(_readAllByType, new SerializationItemCollection(fields));
		}

		/// <inheritdoc />
		public void Save(Security security, bool forced)
		{
			Save(security);
		}

		/// <inheritdoc />
		public override void Save(Security entity)
		{
			_registry.Exchanges.Save(entity.Board.Exchange);
			_registry.ExchangeBoards.Save(entity.Board);

			base.Save(entity);
		}

		/// <summary>
		/// To get identifiers of saved instruments.
		/// </summary>
		/// <returns>IDs securities.</returns>
		public IEnumerable<string> GetSecurityIds()
		{
			if (_readSecurityIds == null)
				return this.Select(s => s.Id);

			var str = _readSecurityIds.ExecuteScalar<string>(new SerializationItemCollection());
			return str.SplitByComma(",", true);
		}

		/// <inheritdoc />
		protected override void OnAdd(Security entity)
		{
			_registry.Exchanges.Save(entity.Board.Exchange);
			_registry.ExchangeBoards.Save(entity.Board);

			base.OnAdd(entity);
		}

		/// <inheritdoc />
		public void Delete(Security security)
		{
			Remove(security);
		}

		/// <inheritdoc />
		public void DeleteBy(Security criteria)
		{
			this.Filter(criteria).ForEach(s => Remove(s));
		}

		void IDisposable.Dispose()
		{
		}
	}
}