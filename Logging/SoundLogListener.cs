namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Windows.Media;

	using Ecng.Serialization;

	/// <summary>
	/// Логгер, проигрывающий музыку при получении сообщения.
	/// </summary>
	public class SoundLogListener : LogListener
	{
		/// <summary>
		/// Создать <see cref="SoundLogListener"/>.
		/// </summary>
		public SoundLogListener()
		{
		}

		/// <summary>
		/// Путь к файлу со звуком, которое будет проиграно при получении сообщения.
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			var player = new MediaPlayer();
			player.Open(new Uri(FileName, UriKind.RelativeOrAbsolute));
			player.Play();
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			FileName = storage.GetValue<string>("FileName");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("FileName", FileName);
		}
	}
}