#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: ControlsGallery.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Ribbon
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Windows;
	using System.Windows.Input;

	using ActiproSoftware.Windows.Controls.Ribbon.Input;

	using Ecng.Common;
	using Ecng.Xaml;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	public partial class ControlsGallery
	{
		public static readonly RoutedCommand AddControlCommand = new RoutedCommand();
		public static readonly RoutedCommand SaveLayoutCommand = new RoutedCommand();
		public static readonly RoutedCommand LoadLayoutCommand = new RoutedCommand();

		public static readonly DependencyProperty SelectedStrategyProperty = DependencyProperty.Register("SelectedStrategy", typeof(StrategyContainer), typeof(ControlsGallery));

		public StrategyContainer SelectedStrategy
		{
			get { return (StrategyContainer)GetValue(SelectedStrategyProperty); }
			set { SetValue(SelectedStrategyProperty, value); }
		}

		public static readonly DependencyProperty ControlTypesProperty = DependencyProperty.Register("ControlTypes", typeof(IEnumerable<ControlType>), typeof(ControlsGallery));

		public IEnumerable<ControlType> ControlTypes
		{
			get { return (IEnumerable<ControlType>)GetValue(ControlTypesProperty); }
			set { SetValue(ControlTypesProperty, value); }
		}

		public ControlsGallery()
		{
			InitializeComponent();

			if (this.IsDesignMode())
				return;

			ControlTypes = AppConfig.Instance.GetControlTypes();
		}

		private static ControlType GetType(object parameter)
		{
			var param = parameter as ObjectValueCommandParameter;

			if (param == null)
				return null;

			var type = (ControlType)param.Value;

			return type;
		}

		private void ExecuteAddControlCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var type = GetType(e.Parameter);

			if (type == null)
				return;

			new OpenWindowCommand(Guid.NewGuid().To<string>(), type.Item1, true).SyncProcess(SelectedStrategy);
		}

		private void CanExecuteAddControlCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			//var type = GetType(e.Parameter);

			//e.CanExecute = type == null || _controls.All(c => c.Control.GetType() != type.Item1);
			e.CanExecute = true;
		}

		private void ExecuteSaveLayoutCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var dlg = new VistaSaveFileDialog
			{
				Filter = LocalizedStrings.Str3584,
				DefaultExt = "xml",
				RestoreDirectory = true
			};

			if (dlg.ShowDialog(Application.Current.GetActiveOrMainWindow()) != true)
				return;

			var cmd = new SaveLayoutCommand();

			cmd.SyncProcess(SelectedStrategy);

			if (!cmd.Layout.IsEmpty())
				File.WriteAllText(dlg.FileName, cmd.Layout);
		}

		private void CanExecuteSaveLayoutCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ExecuteLoadLayoutCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var dlg = new VistaOpenFileDialog
			{
				Filter = LocalizedStrings.Str3584,
				CheckFileExists = true,
				RestoreDirectory = true
			};

			if (dlg.ShowDialog(Application.Current.GetActiveOrMainWindow()) != true)
				return;

			var data = File.ReadAllText(dlg.FileName);

			new LoadLayoutCommand(data).SyncProcess(SelectedStrategy);
		}

		private void CanExecuteLoadLayoutCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}
	}
}
