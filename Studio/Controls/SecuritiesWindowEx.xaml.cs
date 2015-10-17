namespace StockSharp.Studio.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Localization;

	public partial class SecuritiesWindowEx
	{
		public static RoutedCommand SelectSecurityCommand = new RoutedCommand();
		public static RoutedCommand UnselectSecurityCommand = new RoutedCommand();

		/// <summary>
		/// Режим поиска инструментов через <see cref="IConnector.LookupSecurities(StockSharp.BusinessEntities.Security)"/>.
		/// </summary>
		public bool IsLookup { get; set; }

		/// <summary>
		/// Список выбранных инструментов.
		/// </summary>
		public IEnumerable<Security> SelectedSecurities
		{
			get { return SecuritiesSelected.Securities.SyncGet(c => c.ToArray()); }
		}

		/// <summary>
		/// Поставщик информации об инструментах.
		/// </summary>
		public ISecurityProvider SecurityProvider
		{
			get { return SecuritiesAll.SecurityProvider; }
			set { SecuritiesAll.SecurityProvider = value; }
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

		private void SecuritiesWindowEx_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (IsLookup)
			{
				SecuritiesAll.Title = LocalizedStrings.Str3255;
				SecuritiesSelected.Title = LocalizedStrings.Str3256;

				ConfigManager.GetService<IStudioCommandService>().Register<LookupSecuritiesResultCommand>(this, false, cmd =>
				{
					foreach (var security in cmd.Securities)
					{
						// если у нас подключено несколько коннекторов одновременно, то инструменты могут дублироваться
						SecuritiesAll.Securities.TryAdd(security);
					}
				});
			}
			else
			{
				LookupPanel.SetVisibility(false);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			ConfigManager.GetService<IStudioCommandService>().UnRegister<LookupSecuritiesResultCommand>(this);
			base.OnClosed(e);
		}

		public void SelectSecurities(Security[] securities)
		{
			SecuritiesSelected.Securities.AddRange(securities);
			SecuritiesAll.ExcludeSecurities.AddRange(securities);

			EnableOk();
		}

		private void UnselectSecurities(Security[] securities)
		{
			SecuritiesSelected.Securities.RemoveRange(securities);
			SecuritiesAll.ExcludeSecurities.RemoveRange(securities);

			EnableOk();
		}

		private void ExecutedSelectSecurity(object sender, ExecutedRoutedEventArgs e)
		{
			SelectSecurities(SecuritiesAll.SelectedSecurities.ToArray());
		}

		private void CanExecuteSelectSecurity(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SecuritiesAll.SelectedSecurities.Any() || SecuritiesAll.Securities.Count != 0;
		}

		private void ExecutedUnselectSecurity(object sender, ExecutedRoutedEventArgs e)
		{
			UnselectSecurities(SecuritiesSelected.SelectedSecurities.ToArray());
		}

		private void CanExecuteUnselectSecurity(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SecuritiesSelected.SelectedSecurities.Any() || SecuritiesSelected.Securities.Count != 0;
		}

		private void EnableOk()
		{
			Ok.IsEnabled = true;
		}

		private void LookupPanel_OnLookup(Security filter)
		{
			SecuritiesAll.Securities.Clear();
			new LookupSecuritiesCommand(filter).Process(this);
		}
	}
}