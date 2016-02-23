#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: SpeechLogListener.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

			Volume = storage.GetValue<int>(nameof(Volume));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Volume), Volume);
		}
	}
}