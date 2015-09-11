namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.Xaml.Grids;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// A table showing conditional orders (<see cref="Order"/>).
	/// </summary>
	public class OrderConditionalGrid : OrderGrid
	{
		private static readonly string[] _defaultVisibleColumns = { "StopPrice", "Type" };

		private readonly SynchronizedSet<Type> _conditionTypes = new SynchronizedSet<Type>();

		private readonly IList<DataGridColumn> _serializableColumns;

		/// <summary>
		/// Saved columns.
		/// </summary>
		protected override IList<DataGridColumn> SerializableColumns
		{
			get { return _serializableColumns; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderConditionalGrid"/>.
		/// </summary>
		public OrderConditionalGrid()
		{
			_serializableColumns = Columns.ToList();
		}

		/// <summary>
		/// The method is called when a new order added.
		/// </summary>
		/// <param name="order">Order.</param>
		protected override void OnOrderAdded(Order order)
		{
			if (order.Type != OrderTypes.Conditional)
				return;

			Type conditionType;

			lock (_conditionTypes.SyncRoot)
			{
				var condition = order.Condition;

				if (condition == null)
					return;

				conditionType = condition.GetType();

				if (_conditionTypes.Contains(conditionType))
					return;

				_conditionTypes.Add(conditionType);
			}

			GuiDispatcher.GlobalDispatcher.AddAction(() => AddColumns(conditionType));
		}

		private void AddColumns(Type conditionType)
		{
			var properties = conditionType.GetProperties();

			foreach (var property in properties)
			{
				if (property.Name.CompareIgnoreCase("Parameters"))
					continue;

				var name = "Order.Condition." + property.Name;

				if (_serializableColumns.Any(c => c.SortMemberPath.CompareIgnoreCase(name)))
					continue;

				var displayNameAttr = property
					.GetCustomAttributes(typeof(DisplayNameAttribute), false)
					.OfType<DisplayNameAttribute>()
					.FirstOrDefault();

				var column = this.AddTextColumn(name, displayNameAttr != null ? displayNameAttr.DisplayName : property.Name);

				if (!_defaultVisibleColumns.Contains(property.Name))
					column.Visibility = Visibility.Collapsed;

				_serializableColumns.Add(column);
			}
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("ConditionTypes", _conditionTypes.Select(t => t.GetTypeName(false)).ToArray());
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			_conditionTypes.Clear();
			_conditionTypes.AddRange(storage.GetValue<IEnumerable<string>>("ConditionTypes").Select(s => s.To<Type>()));

			_conditionTypes.ForEach(AddColumns);

			base.Load(storage);
		}
	}
}