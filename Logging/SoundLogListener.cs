namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Media;

	using Ecng.Serialization;

	/// <summary>
	/// Logger playing the music when a message received.
	/// </summary>
	public class SoundLogListener : LogListener
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SoundLogListener"/>.
		/// </summary>
		public SoundLogListener()
		{
		}

		/// <summary>
		/// The path to the file with the sound that will be played when a message received.
		/// </summary>
		public string FileName { get; set; }

		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			var player = new MediaPlayer();
			player.Open(new Uri(FileName, UriKind.RelativeOrAbsolute));
			player.Play();

			if (messages.Any(message => message.IsDispose))
				Dispose();
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			FileName = storage.GetValue<string>("FileName");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("FileName", FileName);
		}
	}
}