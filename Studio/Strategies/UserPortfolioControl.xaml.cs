#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Strategies.StrategiesPublic
File: UserPortfolioControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	public partial class UserPortfolioControl : IStudioControl
	{
		public static RoutedCommand AddCommand = new RoutedCommand();
		public static RoutedCommand RemoveCommand = new RoutedCommand();

		private WeighedVirtualPortfolio _virtualPortfolio;

		/// <summary>
		/// Портфели, входящие в корзину.
		/// </summary>
		public ObservableCollection<KeyValuePair<Portfolio, decimal>> InnerPortfolios { set; private get; }

		/// <summary>
		/// Виртуальный счет.
		/// </summary>
		public WeighedVirtualPortfolio VirtualPortfolio
		{
			get { return _virtualPortfolio; }
			set
			{
				_virtualPortfolio = null;

				InnerPortfolios.Clear();
				InnerPortfolios.AddRange(value.InnerPortfolios);

				_virtualPortfolio = value;

				SelectedVirtualPortfolioType = value.GetType();
			}
		}

		/// <summary>
		/// Использовать виртуальный портфель при выставлении заявок.
		/// </summary>
		public bool UseVirtualPortfolio
		{
			get { return CheckBoxUseVirtualPortfolio.IsChecked == true; }
			set { CheckBoxUseVirtualPortfolio.IsChecked = value; }
		}

		/// <summary>
		/// Доступные типы виртуальных счетов.
		/// </summary>
		public ObservableCollection<Tuple<Type, string, string>> VirtualPortfolioTypes { get; private set; }

		/// <summary>
		/// Выбранный тип портфеля.
		/// </summary>
		public Type SelectedVirtualPortfolioType
		{
			get { return (Type)ComboBoxPortfolioTypes.SelectedValue; }
			set { ComboBoxPortfolioTypes.SelectedValue = value; }
		}

		/// <summary>
		/// Событие изменения выбранного типа портфеля.
		/// </summary>
		public event Action<Type> SelectedPortfolioTypeChanged;

		public UserPortfolioControl()
		{
			VirtualPortfolioTypes = new ObservableCollection<Tuple<Type, string, string>>();

			InnerPortfolios = new ObservableCollection<KeyValuePair<Portfolio, decimal>>();
			InnerPortfolios.CollectionChanged += InnerPortfoliosCollectionChanged;

			InitializeComponent();
		}

		private void InnerPortfoliosCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null && _virtualPortfolio != null)
			{
				_virtualPortfolio.InnerPortfolios.AddRange(e.NewItems.Cast<KeyValuePair<Portfolio, decimal>>());
			}

			if (e.OldItems != null && _virtualPortfolio != null)
			{
				_virtualPortfolio.InnerPortfolios.RemoveRange(e.OldItems.Cast<KeyValuePair<Portfolio, decimal>>());
			}

			new ControlChangedCommand(this).Process(this);
		}

		#region IStudioControl

		void IPersistable.Load(SettingsStorage storage)
		{
			UseVirtualPortfolio = storage.GetValue("UseVirtualPortfolio", true);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue("UseVirtualPortfolio", UseVirtualPortfolio);
		}

		void IDisposable.Dispose()
		{
		}

		string IStudioControl.Title
		{
			get { return LocalizedStrings.Str3299; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}

		#endregion

		#region Commands

		private void ExecutedAdd(object sender, ExecutedRoutedEventArgs e)
		{
			InnerPortfolios.Add(new KeyValuePair<Portfolio, decimal>(ComboBoxAllPortfolios.SelectedPortfolio, (decimal)PortfolioWeight.Value));
		}

		private void CanExecuteAdd(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = ComboBoxAllPortfolios.SelectedPortfolio != null && PortfolioWeight.Value.HasValue;
		}

		private void ExecutedRemove(object sender, ExecutedRoutedEventArgs e)
		{
			var item = (KeyValuePair<Portfolio, decimal>)ListBoxPortfolios.SelectedItem;

			InnerPortfolios.Remove(item);
		}

		private void CanExecuteRemove(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = ListBoxPortfolios.SelectedItem != null;
		}

		#endregion

		private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var type = ((Tuple<Type, string, string>)((ComboBox)sender).SelectedItem).Item1;

			if (VirtualPortfolio != null && type == VirtualPortfolio.GetType())
				return;

			SelectedPortfolioTypeChanged.SafeInvoke(type);
			new ControlChangedCommand(this).Process(this);
		}

		private void CheckBoxUseVirtualPortfolio_OnChecked(object sender, RoutedEventArgs e)
		{
			new ControlChangedCommand(this).Process(this);
		}
	}
}