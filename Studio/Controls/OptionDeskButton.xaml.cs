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