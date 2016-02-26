#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: DriveComboBox.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// Визуальный контрол выбора хранилища маркет-данных.
	/// </summary>
	public partial class DriveComboBox
	{
		private class TitledDrive : Disposable, IMarketDataDrive
		{
			private readonly string _name;

			public TitledDrive(string name)
			{
				if (name.IsEmpty())
					throw new ArgumentNullException(nameof(name));

				_name = name;
			}

			void IPersistable.Load(SettingsStorage storage)
			{
				throw new NotSupportedException();
			}

			void IPersistable.Save(SettingsStorage storage)
			{
				throw new NotSupportedException();
			}

			string IMarketDataDrive.Path => _name;

			IMarketDataStorage<NewsMessage> IMarketDataDrive.GetNewsMessageStorage(IMarketDataSerializer<NewsMessage> serializer)
			{
				throw new NotSupportedException();
			}

			ISecurityMarketDataDrive IMarketDataDrive.GetSecurityDrive(Security security)
			{
				throw new NotSupportedException();
			}

			IEnumerable<SecurityId> IMarketDataDrive.AvailableSecurities
			{
				get { throw new NotSupportedException(); }
			}

			IEnumerable<DataType> IMarketDataDrive.GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
			{
				throw new NotSupportedException();
			}

			IMarketDataStorageDrive IMarketDataDrive.GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format)
			{
				throw new NotSupportedException();
			}
		}

		private static readonly IMarketDataDrive _allDrive = new TitledDrive(LocalizedStrings.Str1569);
		private static readonly IMarketDataDrive _selectPath = new TitledDrive(LocalizedStrings.Str2210);

		private IMarketDataDrive _prevSelected;

		/// <summary>
		/// Создать <see cref="DriveComboBox"/>.
		/// </summary>
		public DriveComboBox()
		{
			InitializeComponent();
		}

		private void OnNewDriveCreated(IMarketDataDrive drive)
		{
			this.GuiAsync(() => TryAddDrive(drive));
		}

		private void DriveComboBox_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (this.IsDesignMode())
				return;

			Drives = DriveCache.Instance.Drives;

			if (!Items.Contains(_selectPath))
				Items.Add(_selectPath);

			if (ShowAllDrive)
				TryAddDrive(_allDrive);

			DriveCache.Instance.NewDriveCreated += OnNewDriveCreated;

			if (ShowAllDrive)
				IsAllDrive = true;
			else
				SelectedDrive = _prevSelected ?? Drives.FirstOrDefault();
		}

		private void DriveComboBox_OnUnloaded(object sender, RoutedEventArgs e)
		{
			if (this.IsDesignMode())
				return;

			DriveCache.Instance.NewDriveCreated -= OnNewDriveCreated;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="Drives"/>.
		/// </summary>
		public static DependencyProperty DrivesProperty =
			DependencyProperty.Register(nameof(Drives), typeof(IEnumerable<IMarketDataDrive>),
									typeof(DriveComboBox), new PropertyMetadata(null, DrivesPropertyChanged));

		/// <summary>
		/// Все доступные хранилища маркет-данных.
		/// </summary>
		public IEnumerable<IMarketDataDrive> Drives
		{
			get { return (IEnumerable<IMarketDataDrive>)GetValue(DrivesProperty); }
			set { SetValue(DrivesProperty, value); }
		}

		private static void DrivesPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var comboBox = sender as DriveComboBox;
			if (comboBox == null)
				return;

			((IEnumerable<IMarketDataDrive>)args.NewValue).ForEach(comboBox.TryAddDrive);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="SelectedDrive"/>.
		/// </summary>
		public static DependencyProperty SelectedDriveProperty =
			DependencyProperty.Register(nameof(SelectedDrive), typeof(IMarketDataDrive),
									typeof(DriveComboBox), new PropertyMetadata(null, SelectedDrivePropertyChanged));

		private static void SelectedDrivePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var comboBox = sender as DriveComboBox;
			if (comboBox == null)
				return;

			comboBox.SelectedItem = args.NewValue;
			comboBox._prevSelected = (IMarketDataDrive)args.NewValue;
		}

		/// <summary>
		/// Выбранное хранилище маркет-данных.
		/// </summary>
		public IMarketDataDrive SelectedDrive
		{
			get { return (IMarketDataDrive)GetValue(SelectedDriveProperty); }
			set { SetValue(SelectedDriveProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="ShowAllDrive"/>.
		/// </summary>
		public static DependencyProperty ShowAllDriveProperty =
			DependencyProperty.Register(nameof(ShowAllDrive), typeof(bool),
									typeof(DriveComboBox), new PropertyMetadata(false, ShowAllDriveChanged));

		private static void ShowAllDriveChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var comboBox = sender as DriveComboBox;

			if (comboBox?.Drives == null)
				return;

			if ((bool)args.NewValue)
			{
				comboBox.TryAddDrive(_allDrive);
				comboBox.SelectedDrive = _allDrive;
			}
			else
			{
				comboBox.Items.Remove(_allDrive);
				comboBox.SelectedDrive = comboBox.Drives.FirstOrDefault();
			}
		}

		/// <summary>
		/// Показать хранилище "Все".
		/// </summary>
		public bool ShowAllDrive
		{
			get { return (bool)GetValue(ShowAllDriveProperty); }
			set { SetValue(ShowAllDriveProperty, value); }
		}

		/// <summary>
		/// Выбрано ли хранилище "Все".
		/// </summary>
		public bool IsAllDrive
		{
			get { return SelectedDrive == _allDrive; }
			set { SelectedDrive = value ? _allDrive : _prevSelected; }
		}

		private void TryAddDrive(IMarketDataDrive drive)
		{
			if (!Items.Contains(drive))
				Items.Insert(0, drive);
		}

		/// <summary>
		/// Обработчик события смены выбранного элемента.
		/// </summary>
		/// <param name="e">Параметр события.</param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			var prevDrive = SelectedDrive;
			var currDrive = (IMarketDataDrive)SelectedItem;

			if (currDrive == _selectPath)
			{
				var dlg = new VistaFolderBrowserDialog();

				if (dlg.ShowDialog(this.GetWindow()) == false)
				{
					if (prevDrive == null)
						SelectedIndex = -1;
					else
						SelectedIndex = Items.IndexOf(prevDrive);

					return;
				}

				SelectedDrive = DriveCache.Instance.GetDrive(dlg.SelectedPath);
				TryAddDrive(SelectedDrive);
				SelectedItem = SelectedDrive;
			}
			else
				SelectedDrive = currDrive;

			base.OnSelectionChanged(e);
		}
	}

	/// <summary>
	/// Визуальный редактор для выбора хранилища маркет-данных.
	/// </summary>
	public class DriveComboBoxEditor : TypeEditor<DriveComboBox>
	{
		/// <summary>
		/// Установить <see cref="TypeEditor{T}.ValueProperty"/> значением <see cref="DriveComboBox.SelectedDriveProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = DriveComboBox.SelectedDriveProperty;
		}
	}
}