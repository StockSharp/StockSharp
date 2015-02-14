namespace StockSharp.Xaml
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Logging;

	/// <summary>
	/// Окно для мониторинга работы торговых стратегий.
	/// </summary>
	public partial class MonitorWindow : ILogListener
	{
		/// <summary>
		/// Создать <see cref="MonitorWindow"/>.
		/// </summary>
		public MonitorWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Выводить окно на передний экран в случае ошибки.
		/// </summary>
		public bool BringToFrontOnError { get; set; }

		/// <summary>
		/// Записать сообщение.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		public virtual void WriteMessage(LogMessage message)
		{
			_monitor.WriteMessage(message);

			if (BringToFrontOnError && message.Level == LogLevels.Error)
				this.BringToFront();
		}

		void ILogListener.WriteMessages(IEnumerable<LogMessage> messages)
		{
			((ILogListener)_monitor).WriteMessages(messages);

			if (BringToFrontOnError && messages.Any(message => message.Level == LogLevels.Error))
				this.BringToFront();
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			_monitor.Load(storage);
			BringToFrontOnError = storage.GetValue<bool>("BringToFrontOnError");
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			_monitor.Save(storage);
			storage.SetValue("BringToFrontOnError", BringToFrontOnError);
		}
	}
}