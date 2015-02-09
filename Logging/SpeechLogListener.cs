namespace StockSharp.Logging
{
	using System.Collections.Generic;
	using System.Speech.Synthesis;

	using Ecng.Serialization;

	/// <summary>
	/// Логгер, произносящий слова при получении сообщения.
	/// </summary>
	public class SpeechLogListener : LogListener
	{
		/// <summary>
		/// Создать <see cref="SpeechLogListener"/>.
		/// </summary>
		public SpeechLogListener()
		{
		}

		/// <summary>
		/// Уровень громкости.
		/// </summary>
		public int Volume { get; set; }

		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			using (var speech = new SpeechSynthesizer { Volume = Volume })
			{
				foreach (var message in messages)
					speech.Speak(message.Message);
			}
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Volume = storage.GetValue<int>("Volume");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Volume", Volume);
		}
	}
}