#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: DdeCustomTable.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// Описание DDE экспорта произвольной таблицы.
	/// </summary>
	public sealed class DdeCustomTable
	{
		/// <summary>
		/// Создать <see cref="DdeCustomTable"/>.
		/// </summary>
		/// <param name="entityType">Тип бизнес-объекта.</param>
		/// <remarks>
		/// Тип необходимо пометить атрибутом <see cref="DdeCustomTableAttribute"/> для указания названия Quik таблицы.
		/// </remarks>
		public DdeCustomTable(Type entityType)
			: this(entityType.GetSchema(new DdeCustomTypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)))
		{
		}

		/// <summary>
		/// Создать <see cref="DdeCustomTable"/>.
		/// </summary>
		/// <param name="entityType">Тип бизнес-объекта.</param>
		/// <param name="tableName">Название Quik таблицы, транслируемой по DDE.</param>
		public DdeCustomTable(Type entityType, string tableName)
		{
			Init(entityType.GetSchema(new DdeCustomTypeSchemaFactory(SearchBy.Properties, VisibleScopes.Public)), tableName);
		}

		/// <summary>
		/// Создать <see cref="DdeCustomTable"/>.
		/// </summary>
		/// <param name="schema">Схема бизнес-объекта.</param>
		public DdeCustomTable(Schema schema)
		{
			if (schema == null)
				throw new ArgumentNullException(nameof(schema));

			Init(schema, schema.Name);
		}

		private void Init(Schema schema, string tableName)
		{
			if (schema == null)
				throw new ArgumentNullException(nameof(schema));

			DdeSettings = new DdeSettings();

			Schema = schema;
			TableName = tableName;

			EntitySerializer = new BinarySerializer<int>().GetSerializer(schema.EntityType);
			CollectionSerializer = new BinarySerializer<int>().GetSerializer(typeof(IEnumerable<>).Make(schema.EntityType));

			if (Schema.Identity != null)
				Cache = new SynchronizedDictionary<object, object>();
		}

		/// <summary>
		/// Схема бизнес-объекта.
		/// </summary>
		public Schema Schema { get; private set; }

		/// <summary>
		/// Название таблицы в QUIK.
		/// </summary>
		public string TableName
		{
			get { return DdeSettings.TableName; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				DdeSettings.TableName = value.ToLowerInvariant();
			}
		}

		/// <summary>
		/// Выводить в качестве первой колонки заголовки строк таблицы QUIK.
		/// </summary>
		public bool RowsCaption
		{
			get { return DdeSettings.RowsCaption; }
			set { DdeSettings.RowsCaption = value; }
		}

		/// <summary>
		/// Выводить в качестве первой строки заголовки столбцов таблицы QUIK.
		/// </summary>
		public bool ColumnsCaption
		{
			get { return DdeSettings.ColumnsCaption; }
			set { DdeSettings.ColumnsCaption = value; }
		}

		/// <summary>
		/// Выводить в качестве заголовков их системные (служебные) наименования. Может использоваться для удобства программирования.
		/// </summary>
		public bool FormalValues
		{
			get { return DdeSettings.FormalValues; }
			set { DdeSettings.FormalValues = value; }
		}

		/// <summary>
		/// Оставлять пустыми (не заполнять числовыми значениями) ячейки, содержащие нулевые значения.
		/// </summary>
		public bool EmptyCells
		{
			get { return DdeSettings.EmptyCells; }
			set { DdeSettings.EmptyCells = value; }
		}

		internal DdeSettings DdeSettings { get; private set; }

		internal ISerializer CollectionSerializer { get; private set; }
		internal ISerializer EntitySerializer { get; private set; }
		internal SynchronizedDictionary<object, object> Cache { get; private set; }
	}
}