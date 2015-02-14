namespace StockSharp.Quik
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	enum DdeTableTypes
	{
		None,
		Security,
		Order,
		StopOrder,
		Trade,
		MyTrade,
		Quote,
		EquityPosition,
		DerivativePosition,
		EquityPortfolio,
		DerivativePortfolio,
		CurrencyPortfolio,
	}

	/// <summary>
	/// Метаинформация таблицы, передаваемой под DDE.
	/// Через свойство <see cref="Caption"/> задается, какой заголовок в Quik-e будет иметь таблица.
	/// Например, для таблицы Инструменты по умолчанию в Quik-е используется название 'Таблица текущих значений параметров',
	/// что необходимо переименовать в Инструменты, или же сделать наоборот в самой программе:
	/// <example><code>_trader.SecuritiesTable.Caption = "Таблица текущих значений параметров";</code></example>
	/// <remarks>Дополнительно, через свойство <see cref="Columns"/> можно задать произвольный порядок колонок таблицы.</remarks>
	/// </summary>
	[EntityFactory(typeof(UnitializedEntityFactory<DdeTable>))]
	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Both)]
	[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	public sealed class DdeTable : Equatable<DdeTable>
	{
		internal DdeTable(DdeTableTypes type, string caption, string className, IEnumerable<DdeTableColumn> columns)
		{
			columns.ForEach(c => c.IsMandatory = true);
			Init(type, caption, className, columns);
		}

		private void Init(DdeTableTypes type, string caption, string className, IEnumerable<DdeTableColumn> columns)
		{
			if (caption.IsEmpty())
				throw new ArgumentNullException("caption");

			if (className == null)
				throw new ArgumentNullException("className");

			if (columns == null)
				throw new ArgumentNullException("columns");

			Type = type;
			Caption = caption;
			ClassName = className;

			Columns = new DdeTableColumnList();
			Columns.AddRange(columns);
		}

		internal DdeTableTypes Type { get; private set; }

		/// <summary>
		/// Заголовок таблицы в Quik-e.
		/// </summary>
		public string Caption { get; set; }

		/// <summary>
		/// Класс таблицы в Quik-е.
		/// </summary>
		public string ClassName { get; set; }

		private DdeTableColumnList _columns;

		/// <summary>
		/// Информация о колонках.
		/// </summary>
		/// <remarks>
		/// Если поменялся порядок колонок в Quik-е, необходимо поменять колонки и в программе через <see cref="DdeTable"/>.
		/// Новое значение индекса колонки присваивается следующим образом:
		/// <example><code>// колонка Время расположена 5-ой по счету (нумерация с нуля).
		/// _trader.TradesTable.Columns[4] = DdeTradeColumns.Time;</code></example>
		/// </remarks>
		public DdeTableColumnList Columns
		{
			get { return _columns; }
			private set
			{
				if (value == null)
					throw new ArgumentNullException();

				_columns = value;
				_columns.TableType = () => Type;
			}
		}

		///<summary>
		/// Создать копию объекта <see cref="DdeTable" />.
		///</summary>
		///<returns>Копия.</returns>
		public override DdeTable Clone()
		{
			return new DdeTable(Type, Caption, ClassName, Columns);
		}

		/// <summary>
		/// Сравнить две таблицы на эквивалентность.
		/// </summary>
		/// <param name="other">Другая таблица, с которой необходимо сравнивать.</param>
		/// <returns><see langword="true"/>, если другая таблица равна текущей, иначе, <see langword="false"/>.</returns>
		protected override bool OnEquals(DdeTable other)
		{
			return
				Caption == other.Caption &&
				Type == other.Type &&
				Columns.Count == other.Columns.Count &&
				Columns.SequenceEqual(other.Columns);
		}

		/// <summary>
		/// Рассчитать хеш-код объекта <see cref="DdeTable"/>.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			return Type.GetHashCode() ^ Caption.GetHashCode();
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return Caption;
		}
	}
}