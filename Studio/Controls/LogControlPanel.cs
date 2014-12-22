namespace StockSharp.Studio.Controls
{
	using System.Runtime.InteropServices;

	using Ecng.Serialization;

	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3237Key)]
	[Icon("images/log_24x24.png")]
	[Guid("703384E8-0629-4D77-8771-A92B690AAD5A")]
	public class LogControlPanel : BaseStudioControl
	{
		private readonly LogControl _logControl = new LogControl();
		private readonly GuiLogListener _listener;

		public LogControlPanel()
		{
			Content = _logControl;
			_listener = new GuiLogListener(_logControl);

			WhenLoaded(() => new AddLogListenerCommand(_listener).SyncProcess(this));
		}

		public override void Load(SettingsStorage storage)
		{
			((IPersistable)_logControl).Load(storage);
		}

		public override void Save(SettingsStorage storage)
		{
			((IPersistable)_logControl).Save(storage);
		}

		public override void Dispose()
		{
			new RemoveLogListenerCommand(_listener).Process(this);
		}
	}
}