#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: ExchangeBoardPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;

	using Ecng.Serialization;

	using StockSharp.Localization;

	public partial class ExchangeBoardPane : IPane
	{
		public ExchangeBoardPane()
		{
			InitializeComponent();
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Editor.Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			Editor.Save(storage);
		}

		void IDisposable.Dispose()
		{
		}

		string IPane.Title => LocalizedStrings.Str2831;

		Uri IPane.Icon => null;

		bool IPane.IsValid => true;
	}
}