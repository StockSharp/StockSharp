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
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The table showing financial instruments (<see cref="Security"/>).
	/// </summary>
	public partial class SecurityGrid
	{
		private sealed class SecurityItem : Security
		{
			private Trade _prevTrade;
			private bool _isLastTradeUp;
			private bool _isLastTradeDown;

			public Security Security { get; }

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
					throw new ArgumentNullException(nameof(security));

				Security = security;
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
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;
			}

			private SecurityItem GetItem(Security security)
			{
				if (security == null)
					throw new ArgumentNullException(nameof(security));

				var data = _parent.TryGetItem(security);

				if (data == null)
					throw new InvalidOperationException(LocalizedStrings.Str1548Params);

				return data;
			}

			private IList<object> SelectedItems => (IList<object>)_parent.SelectedItems;

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

			int ICollection<Security>.Count => SelectedItems.Count;

			bool ICollection<Security>.IsReadOnly => false;

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
		/// Initializes a new instance of the <see cref="SecurityGrid"/>.
		/// </summary>
		public SecurityGrid()
		{
			InitializeComponent();

			var itemsSource = new ObservableCollectionEx<SecurityItem>();
			ItemsSource = itemsSource;

			_securities = new ConvertibleObservableCollection<Security, SecurityItem>(new ThreadSafeObservableCollection<SecurityItem>(itemsSource), CreateItem);

			_selectedSecurities = new SelectedSecurityList(this);
		}

		/// <summary>
		/// The selected instrument.
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
		/// All available instruments.
		/// </summary>
		public IListEx<Security> Securities => _securities;

		/// <summary>
		/// Selected instruments.
		/// </summary>
		public IList<Security> SelectedSecurities => _selectedSecurities;

		private IMarketDataProvider _marketDataProvider;

		/// <summary>
		/// The market data provider.
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

			data.ApplyChanges(changes, TimeHelper.NowWithOffset, TimeHelper.Now);
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