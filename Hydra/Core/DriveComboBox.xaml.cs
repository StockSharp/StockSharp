namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Algo.Storages;
	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// Визуальный контрол выбора хранилища маркет-данных.
	/// </summary>
	public partial class DriveComboBox
	{
		private class SelectPathDrive : Disposable, IMarketDataDrive
		{
			void IPersistable.Load(SettingsStorage storage)
			{
				throw new NotSupportedException();
			}

			void IPersistable.Save(SettingsStorage storage)
			{
				throw new NotSupportedException();
			}

			string IMarketDataDrive.Path
			{
				get { return LocalizedStrings.Str2210; }
			}

			IEnumerable<Tuple<Type, object[]>> IMarketDataDrive.GetCandleTypes(SecurityId securityId, StorageFormats format)
			{
				throw new NotSupportedException();
			}

			IMarketDataStorageDrive IMarketDataDrive.GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format)
			{
				throw new NotSupportedException();
			}
		}

		private static readonly SelectPathDrive _selectPath = new SelectPathDrive();
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
			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			Drives = DriveCache.Instance.AllDrives;

			if (!Items.Contains(_selectPath))
				Items.Add(_selectPath);

			DriveCache.Instance.NewDriveCreated += OnNewDriveCreated;

			SelectedDrive = _prevSelected ?? Drives.FirstOrDefault();
		}

		private void DriveComboBox_OnUnloaded(object sender, RoutedEventArgs e)
		{
			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			DriveCache.Instance.NewDriveCreated -= OnNewDriveCreated;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="Drives"/>.
		/// </summary>
		public static DependencyProperty DrivesProperty =
			DependencyProperty.Register("Drives", typeof(IEnumerable<IMarketDataDrive>),
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
			DependencyProperty.Register("SelectedDrive", typeof(IMarketDataDrive),
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