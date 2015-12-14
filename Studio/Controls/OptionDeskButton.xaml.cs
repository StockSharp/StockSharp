#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.ControlsPublic
File: OptionDeskButton.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls
{
	using System;

	using Ecng.Serialization;

	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;

	public partial class OptionDeskButton : IStudioControl
	{
		public OptionDeskButton()
		{
			InitializeComponent();
		}

		protected override void OnClick()
		{
			new OpenWindowCommand(Guid.NewGuid().ToString(), typeof(OptionDeskPanel), false).Process(this);
			base.OnClick();
		}

		void IPersistable.Load(SettingsStorage storage)
		{
		}

		void IPersistable.Save(SettingsStorage storage)
		{
		}

		void IDisposable.Dispose()
		{
		}

		string IStudioControl.Title
		{
			get { return (string)ToolTip; }
		}

		Uri IStudioControl.Icon
		{
			get { return null; }
		}
	}
}