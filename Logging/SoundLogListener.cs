#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: SoundLogListener.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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

			FileName = storage.GetValue<string>(nameof(FileName));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(FileName), FileName);
		}
	}
}