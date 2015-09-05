namespace StockSharp.Logging
{
	using System.Collections.Generic;
	using System.Speech.Synthesis;

	using Ecng.Serialization;

	/// <summary>
	/// Logger speaking words when a message received.
	/// </summary>
	public class SpeechLogListener : LogListener
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SpeechLogListener"/>.
		/// </summary>
		public SpeechLogListener()
		{
		}

		/// <summary>
		/// The volume level.
		/// </summary>
		public int Volume { get; set; }

		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			using (var speech = new SpeechSynthesizer { Volume = Volume })
			{
				foreach (var message in messages)
				{
					if (message.IsDispose)
					{
						Dispose();
						return;
					}

					speech.Speak(message.Message);
				}
			}
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Volume = storage.GetValue<int>("Volume");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Volume", Volume);
		}
	}
}