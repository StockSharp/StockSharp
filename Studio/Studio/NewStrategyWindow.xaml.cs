#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: NewStrategyWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using MoreLinq;

	using Ookii.Dialogs.Wpf;

	using StockSharp.Algo.Strategies;
	using StockSharp.Studio.Core;
	using StockSharp.Localization;

	public partial class NewStrategyWindow
	{
		private readonly StrategyInfo _codeInfo = new StrategyInfo();
		private Assembly _currentAssembly;

		public static RoutedCommand CreateCommand = new RoutedCommand();

		public NewStrategyWindow(IEnumerable<StrategyInfoTypes> types)
		{
			InitializeComponent();

			CodeStrategyTemplateCtrl.ItemsSource = new[]
			{
				new { Name = LocalizedStrings.Str3636, Body = Properties.Resources.NewStrategy },
				new { Name = LocalizedStrings.Str3293, Body = Properties.Resources.SmaStrategy }
			};

			DiagramStrategyTemplateCtrl.ItemsSource = new[]
			{
				new { Name = LocalizedStrings.Str3636, Body = string.Empty },
				new { Name = LocalizedStrings.Str3293, Body = Properties.Resources.SmaDiagramStrategy },
				new { Name = LocalizedStrings.Str3637, Body = Properties.Resources.ArbitrageStrategy }
			};

			CodeAnalyticsTemplateCtrl.ItemsSource = new[]
			{
				new { Name = LocalizedStrings.Str3636, Body = Properties.Resources.NewAnalyticsStrategy },
				new { Name = LocalizedStrings.Str3638, Body = Properties.Resources.DailyHighestVolumeAnalytics },
				new { Name = LocalizedStrings.Str3639, Body = Properties.Resources.PriceVolumeDistributionAnalytics }
			};

			SetTabsVisibility(types);
		}

		public StrategyInfo SelectedInfo => CreateFromDll ? (StrategyInfo)StrategyTypeCtrl.SelectedItem : _codeInfo;

		public bool CreateFromDll => FromDll.IsSelected;

		public bool CreateFromCode => FromCode.IsSelected;

		public bool CreateFromDiagram => FromDiagram.IsSelected;

		public bool CreateFromAnalytics => FromAnalytics.IsSelected;

		private void SetTabsVisibility(IEnumerable<StrategyInfoTypes> types)
		{
			TabControl.Items.OfType<TabItem>().ForEach(t => t.Visibility = Visibility.Collapsed);

			foreach (var type in types)
			{
				switch (type)
				{
					case StrategyInfoTypes.SourceCode:
						FromCode.Visibility = Visibility.Visible;
						break;

					case StrategyInfoTypes.Diagram:
						FromDiagram.Visibility = Visibility.Visible;
						break;

					case StrategyInfoTypes.Assembly:
						FromDll.Visibility = Visibility.Visible;
						break;

					case StrategyInfoTypes.Analytics:
						FromAnalytics.Visibility = Visibility.Visible;
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			TabControl.SelectedItem = TabControl.Items.OfType<TabItem>().First(t => t.Visibility == Visibility.Visible);
		}

		private void FindPath_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaOpenFileDialog { Filter = LocalizedStrings.Str3640 };

			if (!PathCtrl.Text.IsEmpty())
				dlg.FileName = PathCtrl.Text;

			if (dlg.ShowDialog(this) == true)
			{
				var path = dlg.FileName.ToLowerInvariant();
				ReadAssembly(path);
			}
		}

		private void ReadAssembly(string path)
		{
			PathCtrl.Text = path;
			PathCtrl.ToolTip = path;

			var tuple = path.LoadAssembly();

			_currentAssembly = tuple != null ? tuple.Item2 : null;

			RefreshTypes();
		}

		private void RefreshTypes()
		{
			if (_currentAssembly == null)
			{
				StrategyTypeCtrl.ItemsSource = Enumerable.Empty<Type>();
				return;
			}

			var infos = _currentAssembly.GetTypes()
				.Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Strategy)) && (IsPublicOnly.IsChecked != true || t.IsPublic))
				.Select(t => new StrategyInfo
				{
					Name = t.GetDisplayName(),
					Description = t.GetDescription(),
					Type = StrategyInfoTypes.Assembly,
					Path = PathCtrl.Text,
					StrategyType = t,
					Assembly = System.IO.File.ReadAllBytes(PathCtrl.Text)
				})
				.ToArray();

			var info = SelectedInfo;

			StrategyTypeCtrl.ItemsSource = infos;

			if (infos.Length > 0)
			{
				if (info != null)
				{
					var info1 = info;
					info = infos.FirstOrDefault(i => i.StrategyType == info1.StrategyType);
				}

				StrategyTypeCtrl.SelectedIndex = info == null ? 0 : infos.IndexOf(info);
			}
		}

		private void ExecutedCreate(object sender, ExecutedRoutedEventArgs e)
		{
			SelectedInfo.Name = StrategyNameCtrl.Text;

			if (CreateFromCode)
			{
				SelectedInfo.Description = StrategyNameCtrl.Text;
				SelectedInfo.Type = StrategyInfoTypes.SourceCode;
				SelectedInfo.Path = string.Empty;
				SelectedInfo.Body = (string)CodeStrategyTemplateCtrl.SelectedValue;
			}
			else if (CreateFromAnalytics)
			{
				SelectedInfo.Description = StrategyNameCtrl.Text;
				SelectedInfo.Type = StrategyInfoTypes.Analytics;
				SelectedInfo.Path = string.Empty;
				SelectedInfo.Body = (string)CodeAnalyticsTemplateCtrl.SelectedValue;
			}
			else if (CreateFromDiagram)
			{
				SelectedInfo.Description = StrategyNameCtrl.Text;
				SelectedInfo.Type = StrategyInfoTypes.Diagram;
				SelectedInfo.Path = string.Empty;
				SelectedInfo.Body = (string)DiagramStrategyTemplateCtrl.SelectedValue;
			}
			else
			{
				if (ConfigManager.GetService<IStudioEntityRegistry>().Strategies.Any(s => s.StrategyType == SelectedInfo.StrategyType))
				{
					new MessageBoxBuilder()
						.Owner(this)
						.Text(LocalizedStrings.Str3641Params.Put(SelectedInfo.StrategyType))
						.Error()
						.Show();

					return;
				}
			}

			DialogResult = true;
		}

		private void CanExecuteCreate(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !StrategyNameCtrl.Text.IsEmpty() && (!CreateFromDll || SelectedInfo != null);
		}

		private void IsPublicOnly_Click(object sender, RoutedEventArgs e)
		{
			RefreshTypes();
		}
	}
}