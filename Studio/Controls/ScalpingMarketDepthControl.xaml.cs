#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: ScalpingMarketDepthControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace StockSharp.Studio.Controls
{
	using System;
	using System.Globalization;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.MarketDepthKey)]
	[DescriptionLoc(LocalizedStrings.Str3272Key)]
	[Icon("images/marketdepth_24x24.png")]
	public partial class ScalpingMarketDepthControl
	{
		private sealed class Command : ICommand
		{
			private readonly MarketDepthControl _parent;
			private readonly MarketDepthControlAction _action;

			public Command(MarketDepthControl parent, MarketDepthControlAction action)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				if (action == null)
					throw new ArgumentNullException(nameof(action));

				_parent = parent;
				_action = action;

				_parent.SelectionChanged += OnGridSelectionChanged;
			}

			void ICommand.Execute(object parameter)
			{
				_action.Process(MarketDepthColumns.Price, _parent.SelectedQuote);
				//new MarketDepthKeyActionCommand(_action.Key, _action.ModifierKey, MarketDepthColumns.Price, _parent.SelectedQuote).Process(_parent);
			}

			bool ICommand.CanExecute(object parameter)
			{
				return true;
			}

			public event EventHandler CanExecuteChanged;

			private void OnGridSelectionChanged(object sender, EventArgs e)
			{
				CanExecuteChanged.Cast().SafeInvoke(this);
			}
		}

		/// <summary>
		/// Действие-акселлератор для стакана. Задает правила для горячих клавиш и кнопок мышки.
		/// </summary>
		private class MarketDepthControlAction
		{
			private readonly Action<MarketDepthColumns, Quote> _processAction;

			/// <summary>
			/// Создать правило для горячих клавиш.
			/// </summary>
			/// <param name="key">Горячая клавиша.</param>
			/// <param name="modifierKey">Модификатор.</param>
			/// <param name="processAction">Правило.</param>
			public MarketDepthControlAction(Key key, ModifierKeys modifierKey, Action<MarketDepthColumns, Quote> processAction)
			{
				_processAction = processAction;

				Key = key;
				ModifierKey = modifierKey;
			}

			/// <summary>
			/// Создать правило на действие мышки.
			/// </summary>
			/// <param name="mouseAction">Действие мышки.</param>
			/// <param name="modifierKey">Модификатор.</param>
			/// <param name="processAction">Правило.</param>
			public MarketDepthControlAction(MouseAction mouseAction, ModifierKeys modifierKey, Action<MarketDepthColumns, Quote> processAction)
			{
				_processAction = processAction;

				MouseAction = mouseAction;
				ModifierKey = modifierKey;
			}

			public void Process(MarketDepthColumns column, Quote quote)
			{
				if (quote == null)
					return;

				_processAction.SafeInvoke(column, quote);
			}

			/// <summary>
			/// Действие мышки.
			/// </summary>
			public MouseAction MouseAction { get; private set; }

			/// <summary>
			/// Горячая клавиша.
			/// </summary>
			public Key Key { get; private set; }

			/// <summary>
			/// Модификатор.
			/// </summary>
			public ModifierKeys ModifierKey { get; private set; }
		}

		private sealed class MarketDepthControlActionList : BaseList<MarketDepthControlAction>
		{
			private readonly SynchronizedDictionary<Tuple<Key, ModifierKeys>, KeyBinding> _keyBindings = new SynchronizedDictionary<Tuple<Key, ModifierKeys>, KeyBinding>();
			private readonly SynchronizedDictionary<Tuple<MouseAction, ModifierKeys>, MarketDepthControlAction> _mouseActions = new SynchronizedDictionary<Tuple<MouseAction, ModifierKeys>, MarketDepthControlAction>();
			private readonly MarketDepthControl _parent;

			public MarketDepthControlActionList(MarketDepthControl parent)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;
			}

			public void TryInvokeMouse(MarketDepthColumns column, MouseAction action, ModifierKeys modifierKey)
			{
				var value = _mouseActions.TryGetValue(Tuple.Create(action, modifierKey));

				if (value != null)
					value.Process(column, _parent.SelectedQuote);

				//if (value != null && _parent.SelectedQuote != null && _parent.SelectedQuote.Price != 0)
				//	new MarketDepthMouseActionCommand(action, modifierKey, column, _parent.SelectedQuote).Process(_parent);
			}

			protected override bool OnAdding(MarketDepthControlAction item)
			{
				//if (_keyBindings.ContainsKey(item))
				//	throw new ArgumentException(@"Действие уже было ранее добавлено.", "item");

				if (item.Key != System.Windows.Input.Key.None)
				{
					var binding = new KeyBinding(new Command(_parent, item), item.Key, item.ModifierKey);
					_keyBindings[Tuple.Create(item.Key, item.ModifierKey)] = binding;

					GuiDispatcher.GlobalDispatcher.AddAction(() => _parent.InputBindings.Add(binding));
				}
				else
					_mouseActions[Tuple.Create(item.MouseAction, item.ModifierKey)] = item;

				return base.OnAdding(item);
			}

			protected override bool OnRemoving(MarketDepthControlAction item)
			{
				if (item.Key != System.Windows.Input.Key.None)
				{
					var key = Tuple.Create(item.Key, item.ModifierKey);
					var binding = _keyBindings.TryGetValue(key);

					if (binding != null)
					{
						GuiDispatcher.GlobalDispatcher.AddAction(() => _parent.InputBindings.Remove(binding));
						_keyBindings.Remove(key);
					}
					
				}
				else
					_mouseActions.Remove(Tuple.Create(item.MouseAction, item.ModifierKey));

				return base.OnRemoving(item);
			}

			protected override bool OnClearing()
			{
				GuiDispatcher.GlobalDispatcher.AddAction(() => _parent.InputBindings.Clear());
				_keyBindings.Clear();
				_mouseActions.Clear();

				return base.OnClearing();
			}
		}

		private readonly MarketDepthControlActionList _actions;
		private bool _isLoaded;
		private bool _needRequestData;

		public BuySellSettings Settings => BuySellPanel.Settings;

		public ScalpingMarketDepthControl()
		{
			InitializeComponent();

			Settings.SecurityChanged += (oldSec, newSec) =>
			{
				if (!_isLoaded)
					_needRequestData = true;

				if(oldSec != null)
					new RefuseMarketDataCommand(oldSec, MarketDataTypes.MarketDepth).Process(this);

				MdControl.Clear();

				new RequestMarketDataCommand(newSec, MarketDataTypes.MarketDepth).Process(this);

				UpdateTitle();
			};

			//MdControl.Changed += () => new ControlChangedCommand(this).Process();
			MdControl.CanDrag += OnQuotesCanDrag;
			MdControl.Dropping += OnQuotesDropping;

			//Quotes.ColumnsMoved += OnColumnsMoved;
			MdControl.CellMouseLeftButtonUp += OnQuotesCellMouseLeftButtonUp;
			MdControl.CellMouseRightButtonUp += OnQuotesCellMouseRightButtonUp;

			//TODO добавить редактирование дейтсвией в пропгриде
			_actions = new MarketDepthControlActionList(MdControl)
			{
				new MarketDepthControlAction(MouseAction.LeftClick, ModifierKeys.None, (c, q) =>
				{
					switch (c)
					{
						case MarketDepthColumns.Buy:
						case MarketDepthColumns.Sell:
							new RegisterOrderCommand(CreateOrder(c, q)).Process(this);
							break;

						case MarketDepthColumns.OwnBuy:
						case MarketDepthColumns.OwnSell:
							new CancelOrderCommand(CreateOrder(c, q)).Process(this);
							break;
					}
				}),
				new MarketDepthControlAction(System.Windows.Input.Key.Escape, ModifierKeys.None, (c, q) => new CancelAllOrdersCommand().Process(this)),
			};

			MdControl.SelectedCellsChanged += (sender, args) =>
			{
				var q = MdControl.SelectedQuote;
				if(q != null)
					BuySellPanel.LimitPriceCtrl.Value = q.Price;
			};

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			//cmdSvc.Register<SubscribeMarketDepthKeyActionCommand>(this, cmd => _actions.Add(new MarketDepthControlAction(cmd.Key, cmd.ModifierKey)));
			//cmdSvc.Register<SubscribeMarketDepthMouseActionCommand>(this, cmd => _actions.Add(new MarketDepthControlAction(cmd.MouseAction, cmd.ModifierKey)));
			//cmdSvc.Register<UnSubscribeMarketDepthKeyActionCommand>(this, cmd => _actions.Remove(new MarketDepthControlAction(cmd.Key, cmd.ModifierKey)));
			//cmdSvc.Register<UnSubscribeMarketDepthMouseActionCommand>(this, cmd => _actions.Remove(new MarketDepthControlAction(cmd.MouseAction, cmd.ModifierKey)));

			cmdSvc.Register<UpdateMarketDepthCommand>(this, false, cmd =>
			{
				if (Settings.Security == null || cmd.Depth.Security == Settings.Security)
					MdControl.UpdateDepth(cmd.Depth);
			});
			cmdSvc.Register<ClearMarketDepthCommand>(this, true, cmd =>
			{
				MdControl.Clear();

				if(cmd.Security == null || cmd.Security == Settings.Security)
					return;

				Settings.Security = cmd.Security;
			});
			cmdSvc.Register<OrderCommand>(this, false, cmd =>
			{
				if (Settings.Security != null && cmd.Order.Security != Settings.Security)
					return;

				switch (cmd.Action)
				{
					case OrderActions.Registered:
						MdControl.ProcessNewOrder(cmd.Order);
						break;

					case OrderActions.Changed:
						MdControl.ProcessChangedOrder(cmd.Order);
						break;
				}
			});
			cmdSvc.Register<ResetedCommand>(this, false, cmd => new RequestMarketDataCommand(Settings.Security, MarketDataTypes.MarketDepth).Process(this));

			WhenLoaded(() =>
			{
				_isLoaded = true;

				new RequestBindSource(this).SyncProcess(this);

				if (_needRequestData)
					new RequestMarketDataCommand(Settings.Security, MarketDataTypes.MarketDepth).Process(this);
			});
		}

		private void UpdateTitle()
		{
			Title = Settings.Security != null ? Settings.Security.Id : LocalizedStrings.MarketDepth;
		}

		private bool OnQuotesCanDrag(DataGridCell cell)
		{
			return true;
			//var column = MdControl.GetColumnIndex(cell);
			//return column == MarketDepthColumns.OwnBuy || column == MarketDepthColumns.OwnSell || column == MarketDepthColumns.Price;
		}

		private bool OnQuotesDropping(DataGridCell from, DataGridCell to)
		{
			//if (from != to && from.Column == to.Column)
			//	return _listener.Move(MdControl.GetColumnIndex(from), ((MarketDepthQuote)from.DataContext).Quote, ((MarketDepthQuote)to.DataContext).Quote);
			//else
			return false;
		}

		private void OnQuotesCellMouseLeftButtonUp(DataGridCell cell, MouseButtonEventArgs e)
		{
			_actions.TryInvokeMouse(MdControl.GetColumnIndex(cell), e.ClickCount == 1 ? MouseAction.LeftClick : MouseAction.LeftDoubleClick, Keyboard.Modifiers);
		}

		private void OnQuotesCellMouseRightButtonUp(DataGridCell cell, MouseButtonEventArgs e)
		{
			_actions.TryInvokeMouse(MdControl.GetColumnIndex(cell), e.ClickCount == 1 ? MouseAction.RightClick : MouseAction.RightDoubleClick, Keyboard.Modifiers);
		}

		private Order CreateOrder(MarketDepthColumns column, Quote quote)
		{
			if (quote == null)
				throw new ArgumentNullException(nameof(quote));

			Sides direction;

			switch (column)
			{
				case MarketDepthColumns.Buy:
				case MarketDepthColumns.OwnBuy:
					direction = Sides.Buy;
					break;

				case MarketDepthColumns.Sell:
				case MarketDepthColumns.OwnSell:
					direction = Sides.Sell;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return CreateOrder(direction, quote.Price, OrderTypes.Limit);
		}

		private Order CreateOrder(Sides direction, decimal price = 0, OrderTypes type = OrderTypes.Market)
		{
			return new Order
			{
				Portfolio = Settings.Portfolio,
				Security = Settings.Security,
				Direction = direction,
				Price = price,
				Volume = Settings.Volume,
				Type = type,
			};
		}

		public override void Dispose()
		{
			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			//cmdSvc.UnRegister<SubscribeMarketDepthKeyActionCommand>(this);
			//cmdSvc.UnRegister<SubscribeMarketDepthMouseActionCommand>(this);
			//cmdSvc.UnRegister<UnSubscribeMarketDepthKeyActionCommand>(this);
			//cmdSvc.UnRegister<UnSubscribeMarketDepthMouseActionCommand>(this);
			
			cmdSvc.UnRegister<UpdateMarketDepthCommand>(this);
			cmdSvc.UnRegister<ClearMarketDepthCommand>(this);
			cmdSvc.UnRegister<OrderCommand>(this);
			cmdSvc.UnRegister<ResetedCommand>(this);
		}

		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);

			settings.SetValue("GridSettings", MdControl.Save());
			settings.SetValue("BuySellSettings", BuySellPanel.Save());
		}

		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);

			MdControl.Load(settings.GetValue<SettingsStorage>("GridSettings"));

			var buySellSettings = settings.GetValue<SettingsStorage>("BuySellSettings");
			if (buySellSettings != null)
				BuySellPanel.Load(buySellSettings);
		}
	}

	#region MarketDepth commands

	//public abstract class MarketDepthActionCommand : BaseStudioCommand
	//{
	//	protected MarketDepthActionCommand(ModifierKeys modifierKey)
	//	{
	//		ModifierKey = modifierKey;
	//	}

	//	public ModifierKeys ModifierKey { get; private set; }
	//}

	//public class SubscribeMarketDepthMouseActionCommand : MarketDepthActionCommand
	//{
	//	public SubscribeMarketDepthMouseActionCommand(MouseAction mouseAction, ModifierKeys modifierKey = ModifierKeys.None)
	//		: base(modifierKey)
	//	{
	//		MouseAction = mouseAction;
	//	}

	//	public MouseAction MouseAction { get; private set; }
	//}

	//public class UnSubscribeMarketDepthMouseActionCommand : SubscribeMarketDepthMouseActionCommand
	//{
	//	public UnSubscribeMarketDepthMouseActionCommand(MouseAction mouseAction, ModifierKeys modifierKey = ModifierKeys.None)
	//		: base(mouseAction, modifierKey)
	//	{
	//	}
	//}

	//public class SubscribeMarketDepthKeyActionCommand : MarketDepthActionCommand
	//{
	//	public SubscribeMarketDepthKeyActionCommand(Key key, ModifierKeys modifierKey = ModifierKeys.None)
	//		: base(modifierKey)
	//	{
	//		Key = key;
	//	}

	//	public Key Key { get; private set; }
	//}

	//public class UnSubscribeMarketDepthKeyActionCommand : SubscribeMarketDepthKeyActionCommand
	//{
	//	public UnSubscribeMarketDepthKeyActionCommand(Key key, ModifierKeys modifierKey = ModifierKeys.None)
	//		: base(key, modifierKey)
	//	{
	//	}
	//}

	//public class MarketDepthMouseActionCommand : SubscribeMarketDepthMouseActionCommand
	//{
	//	public MarketDepthMouseActionCommand(MouseAction mouseAction, ModifierKeys modifierKey, MarketDepthColumns columns, Quote quote)
	//		: base(mouseAction, modifierKey)
	//	{
	//		Columns = columns;
	//		Quote = quote;
	//	}

	//	public MarketDepthColumns Columns { get; private set; }
	//	public Quote Quote { get; private set; }
	//}

	//public class MarketDepthKeyActionCommand : SubscribeMarketDepthKeyActionCommand
	//{
	//	public MarketDepthKeyActionCommand(Key key, ModifierKeys modifierKey, MarketDepthColumns columns, Quote quote)
	//		: base(key, modifierKey)
	//	{
	//		Columns = columns;
	//		Quote = quote;
	//	}

	//	public MarketDepthColumns Columns { get; private set; }
	//	public Quote Quote { get; private set; }
	//}

	#endregion

	internal class PriceValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var error = LocalizedStrings.Str3273;
			decimal price;

			if(!(value is decimal))
				return new ValidationResult(false, error);

			return (decimal)value <= 0 ? 
				new ValidationResult(false, error) : 
				ValidationResult.ValidResult;
		}
	}
}