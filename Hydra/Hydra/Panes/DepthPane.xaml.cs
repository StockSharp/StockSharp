#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: DepthPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class DepthPane
	{
		private readonly List<QuoteChangeMessage> _loadedDepths = new List<QuoteChangeMessage>();

		public DepthPane()
		{
			InitializeComponent();

			Init(ExportBtn, MainGrid, GetDepths);

			DepthGrid.Columns.RemoveAt(0);
			DepthGrid.Columns.RemoveAt(3);

			DepthGrid.Columns[0].Width = DepthGrid.Columns[2].Width = 50;
		}

		protected override Type DataType => typeof(QuoteChangeMessage);

		public override string Title => LocalizedStrings.MarketDepths + " " + SelectedSecurity;

		public override Security SelectedSecurity
		{
			get { return SelectSecurityBtn.SelectedSecurity; }
			set
			{
				SelectSecurityBtn.SelectedSecurity = value;

				if (value != null)
					DepthGrid.UpdateFormat(value);
			}
		}

		private IEnumerable<QuoteChangeMessage> GetDepths()
		{
			int maxDepth;

			if (!int.TryParse(Depth.Text, out maxDepth))
				maxDepth = int.MaxValue;

			if (maxDepth <= 0)
				maxDepth = 1;

			var interval = TimeSpan.FromMilliseconds(DepthGenerationInterval.Value ?? 0);

			switch (BuildFrom.SelectedIndex)
			{
				case 0:
				{
					var retVal = StorageRegistry
						.GetQuoteMessageStorage(SelectedSecurity, Drive, StorageFormat)
						.Load(From, To + TimeHelper.LessOneDay);

					return retVal
						.Select(md =>
						{
							md.Bids = md.Bids.Take(maxDepth).ToArray();
							md.Asks = md.Asks.Take(maxDepth).ToArray();
							return md;
						})
						.WhereWithPrevious((prev, curr) => (curr.ServerTime - prev.ServerTime) >= interval);
				}

				case 1:
				{
					return StorageRegistry
						.GetOrderLogMessageStorage(SelectedSecurity, Drive, StorageFormat)
						.Load(From + new TimeSpan(18, 45, 0), To + TimeHelper.LessOneDay + new TimeSpan(18, 45, 0))
						// TODO
						.ToMarketDepths(OrderLogBuilders.Plaza2.CreateBuilder(SelectedSecurity.ToSecurityId()), interval, maxDepth);	
				}

				case 2:
				{
					var level1 = StorageRegistry
						.GetLevel1MessageStorage(SelectedSecurity, Drive, StorageFormat)
						.Load(From, To + TimeHelper.LessOneDay);

					return level1.ToOrderBooks();
				}

				default:
					throw new InvalidOperationException();
			}
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			if (!CheckSecurity())
				return;

			int maxDepth;

			DepthGrid.MaxDepth = int.TryParse(Depth.Text, out maxDepth)
				? maxDepth 
				: MarketDepthControl.DefaultDepth;

			var isFirstAdded = false;

			_loadedDepths.Clear();
			Progress.Load(GetDepths(), _loadedDepths.AddRange, 1000, item =>
			{
				QuotesSlider.Maximum = _loadedDepths.Count - 1;

				if (isFirstAdded)
					return;

				DisplayDepth();
				isFirstAdded = true;
			});

			QuotesSlider.Maximum = 0;
			QuotesSlider.Minimum = 0;
			QuotesSlider.SmallChange = 1;
			QuotesSlider.LargeChange = 5;
		}

		private void QuotesSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			DisplayDepth();
		}

		private void DisplayDepth()
		{
			var index = (int)QuotesSlider.Value;

			if (_loadedDepths.Count < (index + 1))
				return;

			var depth = _loadedDepths[index];

			DepthGrid.UpdateDepth(depth);
			DepthDate.Text = depth.ServerTime.ToString("yyyy.MM.dd HH:mm:ss.fff");
		}

		protected override bool CanDirectExport => BuildFrom.SelectedIndex == 0;

		//protected override void OnClosed(EventArgs e)
		//{
		//    Progress.Stop();
		//    base.OnClosed(e);
		//}

		private void SelectSecurityBtn_SecuritySelected()
		{
			if (SelectedSecurity == null)
			{
				ExportBtn.IsEnabled = false;
			}
			else
			{
				ExportBtn.IsEnabled = true;
				UpdateTitle();
				DepthGrid.UpdateFormat(SelectedSecurity);
			}
		}

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			DepthGrid.Load(storage.GetValue<SettingsStorage>(nameof(DepthGrid)));
			Depth.SelectedIndex = storage.GetValue<int>(nameof(Depth));

			if (storage.ContainsKey(nameof(DepthGenerationInterval)))
				DepthGenerationInterval.Value = storage.GetValue<int>(nameof(DepthGenerationInterval));

			BuildFrom.SelectedIndex = storage.GetValue<int>(nameof(BuildFrom));
		}

		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(DepthGrid), DepthGrid.Save());
			storage.SetValue(nameof(Depth), Depth.SelectedIndex);

			if (DepthGenerationInterval.Value != null)
				storage.SetValue(nameof(DepthGenerationInterval), (int)DepthGenerationInterval.Value);

			storage.SetValue(nameof(BuildFrom), BuildFrom.SelectedIndex);
		}
	}
}
