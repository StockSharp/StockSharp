#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: NewsPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	public partial class NewsPane
	{
		public NewsPane()
		{
			InitializeComponent();

			Init(ExportBtn, MainGrid, GetNews);
		}

		protected override Type DataType => typeof(NewsMessage);

		public override string Title => LocalizedStrings.News;

		public override Security SelectedSecurity
		{
			get { return null; }
			set { }
		}

		private void OnDateValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			Progress.ClearStatus();
		}

		private IEnumerable<NewsMessage> GetNews()
		{
			return ConfigManager
				.GetService<IStorageRegistry>()
				.GetNewsMessageStorage(Drive, StorageFormat)
				.Load(From, To + TimeHelper.LessOneDay);
		}

		private void Find_OnClick(object sender, RoutedEventArgs e)
		{
			NewsPanel.NewsGrid.Messages.Clear();
			Progress.Load(GetNews(), NewsPanel.NewsGrid.Messages.AddRange, 1000);
		}

		protected override bool CheckSecurity()
		{
			return true;
		}
	}
}