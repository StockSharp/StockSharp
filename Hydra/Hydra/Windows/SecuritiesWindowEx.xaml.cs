#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Windows.HydraPublic
File: SecuritiesWindowEx.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Windows
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.Algo.History;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Logging;
	using StockSharp.Localization;

	public partial class SecuritiesWindowEx
	{
		public static RoutedCommand SelectSecurityCommand = new RoutedCommand();
		public static RoutedCommand UnselectSecurityCommand = new RoutedCommand();
		private bool _isClosed;

		public IEnumerable<Security> SelectedSecurities
		{
			get { return SecuritiesSelected.Securities.SyncGet(c => c.ToArray()); }
		}

		public ISecurityProvider SecurityProvider
		{
			get { return SecuritiesAll.SecurityProvider; }
			set { SecuritiesAll.SecurityProvider = value; }
		}

		private IHydraTask _task;

		public IHydraTask Task
		{
			get { return _task; }
			set
			{
				_task = value;
				NonLookupWarning.Visibility = _task is ISecurityDownloader ? Visibility.Collapsed : Visibility.Visible;
			}
		}

		public SecuritiesWindowEx()
		{
			InitializeComponent();
			
			SecuritiesAll.SecurityDoubleClick += security =>
			{
				if (security != null)
					SelectSecurities(new[] { security });
			};

			SecuritiesSelected.SecurityDoubleClick += security =>
			{
				if (security != null)
					UnselectSecurities(new[] { security });
			};
		}

		public void SelectSecurities(Security[] securities)
		{
			SecuritiesSelected.Securities.AddRange(securities);
			SecuritiesAll.ExcludeSecurities.AddRange(securities);
		}

		private void UnselectSecurities(Security[] securities)
		{
			SecuritiesSelected.Securities.RemoveRange(securities);
			SecuritiesAll.ExcludeSecurities.RemoveRange(securities);
		}

		private void ExecutedSelectSecurity(object sender, ExecutedRoutedEventArgs e)
		{
			SelectSecurities(SecuritiesAll.SelectedSecurities.ToArray());
		}

		private void CanExecuteSelectSecurity(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SecuritiesAll.SelectedSecurities.Any();
		}

		private void ExecutedUnselectSecurity(object sender, ExecutedRoutedEventArgs e)
		{
			UnselectSecurities(SecuritiesSelected.SelectedSecurities.ToArray());
		}

		private void CanExecuteUnselectSecurity(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SecuritiesSelected.SelectedSecurities.Any();
		}

		private void LookupPanel_OnLookup(Security filter)
		{
			SecuritiesAll.SecurityFilter = filter.Code;

			var downloader = Task as ISecurityDownloader;

			if (downloader == null) 
				return;

			BusyIndicator.BusyContent = LocalizedStrings.Str2834;
			BusyIndicator.IsBusy = true;

			ThreadingHelper
				.Thread(() =>
				{
					var securities = ConfigManager.GetService<IEntityRegistry>().Securities;
					
					try
					{
						downloader.Refresh(securities, filter, s => { }, () => _isClosed);
					}
					catch (Exception ex)
					{
						ex.LogError();
					}

					try
					{
						securities.DelayAction.WaitFlush();
					}
					catch (Exception ex)
					{
						ex.LogError();
					}

					try
					{
						this.GuiAsync(() => BusyIndicator.IsBusy = false);
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
				})
				.Launch();
		}

		private void CreateSecurity_OnClick(object sender, RoutedEventArgs e)
		{
			new SecurityEditWindow { Security = new Security() }.ShowModal(this);
			//var wnd = new SecurityEditWindow { Security = new Security() };

			//if (!wnd.ShowModal(this))
			//	return;

			//AllSecurities.Add(wnd.Security);
		}

		protected override void OnClosed(EventArgs e)
		{
			_isClosed = true;
			base.OnClosed(e);
		}
	}
}