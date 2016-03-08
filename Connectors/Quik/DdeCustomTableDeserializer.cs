#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: DdeCustomTableDeserializer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using Wintellect.PowerCollections;

	using StockSharp.Localization;

	class DdeCustomTableDeserializer
	{
		public class DdeCustomTableList : SynchronizedSet<DdeCustomTable>
		{
			private readonly MultiDictionary<Type, DdeCustomTable> _customTablesByType = new MultiDictionary<Type, DdeCustomTable>(false);
			private readonly Dictionary<string, DdeCustomTable> _customTablesByCaption = new Dictionary<string, DdeCustomTable>();

			protected override bool OnAdding(DdeCustomTable item)
			{
				_customTablesByType.Add(item.Schema.EntityType, item);
				_customTablesByCaption.Add(item.TableName, item);
				return base.OnAdding(item);
			}

			protected override bool OnRemoving(DdeCustomTable item)
			{
				_customTablesByType.Remove(item.Schema.EntityType, item);
				_customTablesByCaption.Remove(item.TableName);
				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				_customTablesByType.Clear();
				_customTablesByCaption.Clear();
				return base.OnClearing();
			}

			public DdeCustomTable GetTable(Type entityType)
			{
				if (entityType == null)
					throw new ArgumentNullException(nameof(entityType));

				lock (SyncRoot)
				{
					var tables = _customTablesByType[entityType];

					if (tables.Count == 0)
						throw new ArgumentException(LocalizedStrings.Str1702Params.Put(entityType.Name), nameof(entityType));

					return tables.First();
				}
			}

			public DdeCustomTable GetTable(string tableName)
			{
				lock (SyncRoot)
					return _customTablesByCaption.TryGetValue(tableName);
			}
		}

		public DdeCustomTableDeserializer()
		{
			CustomTables = new DdeCustomTableList();
		}

		public event Action<Type, IEnumerable<object>> NewCustomTables;
		public event Action<Type, IEnumerable<object>> CustomTablesChanged;

		public DdeCustomTableList CustomTables { get; }

		public bool TryDeserialize(string category, IList<IList<object>> rows)
		{
			if (category.IsEmpty())
				throw new ArgumentNullException(nameof(category));

			if (rows == null)
				throw new ArgumentNullException(nameof(rows));

			var table = CustomTables.GetTable(category);

			if (table != null)
			{
				var schema = table.Schema;

				var changedEntities = new List<object>();

				if (table.Cache != null)
				{
					foreach (var row in rows.ToArray())
					{
						var id = schema.Identity.Factory.CreateInstance(table.EntitySerializer, ToSource(schema.Identity, row));
						var cachedEntity = table.Cache.TryGetValue(id);
						if (cachedEntity != null)
						{
							table.EntitySerializer.Deserialize(ToSource(schema, row), schema.Fields, cachedEntity);
							rows.Remove(row);
							changedEntities.Add(cachedEntity);
						}
					}
				}

				if (rows.Count > 0)
				{
					var newEntities = ((IEnumerable)table.CollectionSerializer.Deserialize(new SerializationItemCollection(
						rows.Select((row, index) => new SerializationItem(
								new VoidField(index.ToString(), schema.EntityType),
								new SerializationItemCollection(ToSource(schema, row))))))).Cast<object>();

					if (schema.Identity != null)
					{
						newEntities.ForEach(e => table.Cache.SafeAdd(schema.Identity.Accessor.GetValue(e), key => e));
					}

					NewCustomTables?.Invoke(schema.EntityType, newEntities);
				}

				if (changedEntities.Count > 0)
					CustomTablesChanged?.Invoke(schema.EntityType, changedEntities);

				return true;
			}
			else
				return false;
		}

		private static SerializationItemCollection ToSource(Schema schema, IList<object> row)
		{
			if (schema == null)
				throw new ArgumentNullException(nameof(schema));

			if (row == null)
				throw new ArgumentNullException(nameof(row));

			return new SerializationItemCollection(schema.Fields.Select(field => ToSource(field, row)));
		}

		private static SerializationItem ToSource(Field field, IList<object> row)
		{
			if (field == null)
				throw new ArgumentNullException(nameof(field));

			if (row == null)
				throw new ArgumentNullException(nameof(row));

			if (field.IsInnerSchema())
			{
				return new SerializationItem(field, ToSource(field.Type.GetSchema(), row));
			}

			if (field.OrderedIndex >= row.Count)
				throw new ArgumentOutOfRangeException(nameof(row), LocalizedStrings.Str1703Params.Put(field.Schema.Name, field.Name, row.Count, field.OrderedIndex));

			return new SerializationItem(field, row[field.OrderedIndex]);
		}
	}
}