namespace StockSharp.Xaml
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Таблица, отображающая финансовые инструменты (<see cref="Security"/>).
	/// </summary>
	public partial class SecurityGrid
	{
		private sealed class SecurityItem : Security
		{
			private Trade _prevTrade;
			private bool _isLastTradeUp;
			private bool _isLastTradeDown;

			public Security Security { get; private set; }

			public bool IsLastTradeUp
			{
				get { return _isLastTradeUp; }
				set
				{
					if (_isLastTradeUp == value)
						return;

					_isLastTradeUp = value;
					Notify("IsLastTradeUp");
				}
			}

			public bool IsLastTradeDown
			{
				get { return _isLastTradeDown; }
				set
				{
					if (_isLastTradeDown == value)
						return;

					_isLastTradeDown = value;
					Notify("IsLastTradeDown");
				}
			}

			public SecurityItem(Security security)
			{
				if (security == null)
					throw new ArgumentNullException("security");

				Security = security;

				Id = security.Id;
				Code = security.Code;
				Name = security.Name;
				ShortName = security.ShortName;
				Board = security.Board;
				Class = security.Class;
				UnderlyingSecurityId = security.UnderlyingSecurityId;
				Type = security.Type;
				OptionType = security.OptionType;
				ExternalId = security.ExternalId;
				SettlementDate = security.SettlementDate;
				Currency = security.Currency;
				ExpiryDate = security.ExpiryDate;
				Multiplier = security.Multiplier;
				Strike = security.Strike;
				BinaryOptionType = security.BinaryOptionType;
			}

			public void RefreshLastTradeDirection()
			{
				if (LastTrade == null)
					return;

				if (_prevTrade == null)
				{
					_prevTrade = LastTrade;
					return;
				}

				if (_prevTrade.Price != LastTrade.Price)
				{
					IsLastTradeUp = false;
					IsLastTradeDown = false;
				}

				if (_prevTrade.Price < LastTrade.Price)
					IsLastTradeUp = true;

				if (_prevTrade.Price > LastTrade.Price)
					IsLastTradeDown = true;

				_prevTrade = LastTrade;
			}
		}

		private sealed class SelectedSecurityList : IList<Security>
		{
			private readonly SecurityGrid _parent;

			public SelectedSecurityList(SecurityGrid parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
			}

			private SecurityItem GetItem(Security security)
			{
				if (security == null)
					throw new ArgumentNullException("security");

				var data = _parent.TryGetItem(security);

				if (data == null)
					throw new InvalidOperationException(LocalizedStrings.Str1548Params);

				return data;
			}

			private IList<object> SelectedItems
			{
				get { return (IList<object>)_parent.SelectedItems; }
			}

			void ICollection<Security>.Add(Security item)
			{
				SelectedItems.Add(GetItem(item));
			}

			void ICollection<Security>.Clear()
			{
				SelectedItems.Clear();
			}

			bool ICollection<Security>.Contains(Security item)
			{
				return SelectedItems.Contains(GetItem(item));
			}

			void ICollection<Security>.CopyTo(Security[] array, int arrayIndex)
			{
				var temp = new object[SelectedItems.Count];
				SelectedItems.CopyTo(temp, arrayIndex);

				foreach (var item in temp.Cast<SecurityItem>())
				{
					array[arrayIndex] = item.Security;
					arrayIndex++;
				}
			}

			bool ICollection<Security>.Remove(Security item)
			{
				return SelectedItems.Remove(GetItem(item));
			}

			int ICollection<Security>.Count
			{
				get { return SelectedItems.Count; }
			}

			bool ICollection<Security>.IsReadOnly
			{
				get { return false; }
			}

			public IEnumerator<Security> GetEnumerator()
			{
				return SelectedItems.Cast<SecurityItem>().Select(i => i.Security).GetEnumerator();
			}

			int IList<Security>.IndexOf(Security item)
			{
				return SelectedItems.IndexOf(GetItem(item));
			}

			void IList<Security>.Insert(int index, Security item)
			{
				SelectedItems.Insert(index, GetItem(item));
			}

			void IList<Security>.RemoveAt(int index)
			{
				SelectedItems.RemoveAt(index);
			}

			Security IList<Security>.this[int index]
			{
				get
				{
					return ((SecurityItem)SelectedItems[index]).Security;
				}
				set
				{
					SelectedItems[index] = GetItem(value);
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		private readonly SelectedSecurityList _selectedSecurities;
		private readonly ConvertibleObservableCollection<Security, SecurityItem> _securities;

		/// <summary>
		/// Создать <see cref="SecurityGrid"/>.
		/// </summary>
		public SecurityGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<SecurityItem>();
			//var itemsSource = new ObservableCollection<SecurityItem>();
			//itemsSource.CollectionChanged += (sender, args) =>
			//{
			//	var newCount = args.NewItems == null ? 0 : args.NewItems.Count;
			//	var oldCount = args.OldItems == null ? 0 : args.OldItems.Count;
			//	Console.WriteLine("{0:X4} ColChange={1} NewCnt={2} OldCnt={3} NewIdx={4} OldIdx={5}",
			//		GetHashCode(), args.Action, newCount, oldCount,
			//		args.NewStartingIndex, args.OldStartingIndex);
			//};
			//((INotifyPropertyChanged)itemsSource).PropertyChanged += (sender, args) =>
			//{
			//	Console.WriteLine("{0:X4} PropChange {1}", GetHashCode(), args.PropertyName);
			//};
			ItemsSource = itemsSource;

			_securities = new ConvertibleObservableCollection<Security, SecurityItem>(new ThreadSafeObservableCollection<SecurityItem>(itemsSource), CreateItem);

			_selectedSecurities = new SelectedSecurityList(this);

			MarketDataProvider = ConfigManager.TryGetService<IMarketDataProvider>();

			if (MarketDataProvider == null)
			{
				ConfigManager.ServiceRegistered += (t, s) =>
				{
					if (MarketDataProvider != null || typeof(IMarketDataProvider) != t)
						return;

					MarketDataProvider = (IMarketDataProvider)s;

					itemsSource.ForEach(item => UpdateData(item.Security, item));
				};
			}
		}

		/// <summary>
		/// Выбранный инструмент.
		/// </summary>
		public Security SelectedSecurity
		{
			get
			{
				var item = (SecurityItem)SelectedItem;
				return item == null ? null : item.Security;
			}
			set
			{
				if (SelectionMode != DataGridSelectionMode.Single)
					SelectedItems.Clear();

				var displayItem = value == null ? null : TryGetItem(value);

				SelectedItem = displayItem;

				if (displayItem == null)
					return;

				ScrollIntoView(displayItem);

				var item = (UIElement)ItemContainerGenerator.ContainerFromItem(displayItem);

				if (item != null)
					item.Focus();
			}
		}

		/// <summary>
		/// Все доступные инструменты.
		/// </summary>
		public IListEx<Security> Securities
		{
			get { return _securities; }
		}

		/// <summary>
		/// Выбранные инструменты.
		/// </summary>
		public IList<Security> SelectedSecurities
		{
			get { return _selectedSecurities; }
		}

		private IMarketDataProvider _marketDataProvider;

		/// <summary>
		/// Поставщик маркет-данных.
		/// </summary>
		public IMarketDataProvider MarketDataProvider
		{
			get { return _marketDataProvider; }
			set
			{
				if (value == _marketDataProvider)
					return;

				if (_marketDataProvider != null)
					_marketDataProvider.ValuesChanged -= MarketDataProviderOnValuesChanged;

				_marketDataProvider = value;

				if (_marketDataProvider != null)
					_marketDataProvider.ValuesChanged += MarketDataProviderOnValuesChanged;
			}
		}

		private void MarketDataProviderOnValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTime localTime)
		{
			var data = TryGetItem(security);

			if (data == null)
				return;

			data.ApplyChanges(changes, serverTime, localTime);
			data.RefreshLastTradeDirection();
		}

		private SecurityItem TryGetItem(Security security)
		{
			return _securities.TryGet(security);
		}

		private SecurityItem CreateItem(Security security)
		{
			var data = new SecurityItem(security);

			if (MarketDataProvider != null)
				UpdateData(security, data);

			return data;
		}

		private void UpdateData(Security security, SecurityItem data)
		{
			var changes = MarketDataProvider.GetSecurityValues(security);

			if (changes == null)
				return;

			data.ApplyChanges(changes, TimeHelper.Now, TimeHelper.Now);
			data.RefreshLastTradeDirection();
		}

		internal Security CurrentSecurity
		{
			get
			{
				var item = (SecurityItem)CurrentItem;
				return item == null ? null : item.Security;
			}
		}
	}
}