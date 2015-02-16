namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using StockSharp.Localization;

	/// <summary>
	/// Коллекция метаданных о колонках таблиц, экспортируемых через DDE.
	/// </summary>
	public class DdeTableColumnList : SynchronizedSet<DdeTableColumn>
	{
		internal DdeTableColumnList()
			: base(true)
		{
		}

		internal Func<DdeTableTypes> TableType { get; set; }

		private readonly SynchronizedSet<DdeTableColumn> _nonMandatoryColumns = new SynchronizedSet<DdeTableColumn>();

		internal ICollection<DdeTableColumn> NonMandatoryColumns
		{
			get { return _nonMandatoryColumns; }
		}

		private readonly SynchronizedSet<DdeTableColumn> _extendedColumns = new SynchronizedSet<DdeTableColumn>();

		internal ICollection<DdeTableColumn> ExtendedColumns
		{
			get { return _extendedColumns; }
		}

		/// <summary>
		/// Найти колонку по текстовому названию.
		/// </summary>
		/// <param name="name">Название колонки.</param>
		/// <returns>Найденная колонка. Если колонка не найдена, то будет возвращено значение null.</returns>
		public DdeTableColumn this[string name]
		{
			get { return this.FirstOrDefault(c => c.Name == name); }
		}

		/// <summary>
		/// Добавление колонки.
		/// </summary>
		/// <param name="item">Колонка.</param>
		/// <returns><see langword="true"/>, если колонку можно добавить. Иначе, <see langword="false"/>.</returns>
		protected override bool OnAdding(DdeTableColumn item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			// разные QuikTrader могут иметь свои настройки и уникальные значения для DdeTableColumn.Index
			//
			// mika свойство Index в последствии было удалено
			//item = item.Clone();

			if (TableType != null)
			{
				if (item.TableType != DdeTableTypes.None)
				{
					if (item.TableType != TableType())
						throw new ArgumentException(LocalizedStrings.Str1704Params.Put(item.Name), "item");
				}
				else
					item.TableType = TableType();
			}

			if (Contains(item))
				throw new ArgumentException(LocalizedStrings.Str1705Params.Put(item.Name), "item");

			TryAddAsExtended(item);

			return base.OnAdding(item);
		}

		/// <summary>
		/// Удаление колонки.
		/// </summary>
		/// <param name="item">Колонка.</param>
		/// <returns><see langword="true"/>, если колонку можно удалить. Иначе, <see langword="false"/>.</returns>
		protected override bool OnRemoving(DdeTableColumn item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			if (item.IsMandatory)
				throw new ArgumentException(LocalizedStrings.Str1706Params.Put(item.Name));

			return base.OnRemoving(item);
		}

		/// <summary>
		/// Удаление колонки.
		/// </summary>
		/// <param name="item">Колонка.</param>
		protected override void OnRemoved(DdeTableColumn item)
		{
			_extendedColumns.Remove(item);

			base.OnRemoved(item);
		}

        /// <summary>
		/// Вставка колонки.
		/// </summary>
		/// <param name="index">Индекс.</param>
		/// <param name="item">Колонка.</param>
		/// <returns><see langword="true"/>, если колонку можно вставить. Иначе, <see langword="false"/>.</returns>
		protected override bool OnInserting(int index, DdeTableColumn item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

			if (Contains(item))
				throw new ArgumentException(LocalizedStrings.Str1705Params.Put(item.Name), "item");

			// разные QuikTrader могут иметь свои настройки и уникальные значения для DdeTableColumn.Index
			//
			// mika свойство Index в последствии было удалено
			//item = item.Clone();

			TryAddAsExtended(item);

			return base.OnInserting(index, item);
		}

		/// <summary>
		/// Удаление всех колонок.
		/// </summary>
		/// <returns><see langword="true"/>, если можно удалить все колонки. Иначе, <see langword="false"/>.</returns>
		protected override bool OnClearing()
		{
			throw new NotSupportedException(LocalizedStrings.Str1707);
		}

		// опциональные колонки, которые записываются напрямую в поля сущностей (например, Security.TheorPrice), а не через ExtensionInfo
		private void TryAddAsExtended(DdeTableColumn column)
		{
			if (column == null)
				throw new ArgumentNullException("column");

			if (column.IsMandatory)
				return;

			var isStandard = false;
			var tableType = TableType == null ? DdeTableTypes.None : TableType();
			
			switch (tableType)
			{
				case DdeTableTypes.Security:
					isStandard =
						column == DdeSecurityColumns.MarginBuy ||
						column == DdeSecurityColumns.MarginSell ||
						column == DdeSecurityColumns.MinPrice ||
						column == DdeSecurityColumns.MaxPrice ||
						column == DdeSecurityColumns.ExpiryDate ||
						column == DdeSecurityColumns.SettlementDate ||
						column == DdeSecurityColumns.LastTradeTime ||
						column == DdeSecurityColumns.LastChangeTime ||
						column == DdeSecurityColumns.LastTradePrice ||
						column == DdeSecurityColumns.LastTradeVolume ||
						column == DdeSecurityColumns.LastTradeVolume2 ||
						column == DdeSecurityColumns.StepPrice ||
						column == DdeSecurityColumns.ShortName ||
						column == DdeSecurityColumns.BestBidPrice ||
						column == DdeSecurityColumns.BestBidVolume ||
						column == DdeSecurityColumns.BestAskPrice ||
						column == DdeSecurityColumns.BestAskVolume ||
						column == DdeSecurityColumns.OpenPrice ||
						column == DdeSecurityColumns.HighPrice ||
						column == DdeSecurityColumns.LowPrice ||
						column == DdeSecurityColumns.ClosePrice ||
						column == DdeSecurityColumns.OpenPositions ||
						column == DdeSecurityColumns.NominalCurrency ||
						column == DdeSecurityColumns.BidsCount ||
						column == DdeSecurityColumns.BidsVolume ||
						column == DdeSecurityColumns.AsksCount ||
						column == DdeSecurityColumns.AsksVolume ||
						column == DdeSecurityColumns.ISIN ||
						column == DdeSecurityColumns.TheorPrice ||
						column == DdeSecurityColumns.ImpliedVolatility ||
						column == DdeSecurityColumns.Strike;
					break;

				case DdeTableTypes.Trade:
					isStandard = column == DdeTradeColumns.TimeMcs;
					break;

				case DdeTableTypes.Order:
					isStandard = 
						column == DdeOrderColumns.TimeMcs ||
						column == DdeOrderColumns.CancelTimeMcs;
					break;

				case DdeTableTypes.MyTrade:
					isStandard = column == DdeMyTradeColumns.TimeMcs;
					break;
			}

			if (!isStandard)
				_extendedColumns.Add(column);

			_nonMandatoryColumns.Add(column);
		}
	}
}