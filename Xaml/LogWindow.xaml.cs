namespace StockSharp.Xaml
{
	using System.Collections.Generic;

	using Ecng.Serialization;

	using StockSharp.Logging;

	/// <summary>
	/// Окно для отображения логов.
	/// </summary>
	public partial class LogWindow : ILogListener
	{
		/// <summary>
		/// Создать окно.
		/// </summary>
		public LogWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Формат конвертирования времени в строку.
		/// </summary>
		public string TimeFormat
		{
			get { return LogCtrl.TimeFormat; }
			set { LogCtrl.TimeFormat = value; }
		}

		/// <summary>
		/// Коллекция лог-записей.
		/// </summary>
		public IList<LogMessage> Messages
		{
			get { return LogCtrl.Messages; }
		}

		void ILogListener.WriteMessages(IEnumerable<LogMessage> messages)
		{
			((ILogListener)LogCtrl).WriteMessages(messages);
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			LogCtrl.Load(storage);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			LogCtrl.Save(storage);
		}
	}
}