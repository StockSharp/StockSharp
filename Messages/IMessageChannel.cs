#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: IMessageChannel.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// States <see cref="IMessageChannel"/>.
	/// </summary>
	public enum ChannelStates
	{
		/// <summary>
		/// Stopped.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1128Key)]
		Stopped,

		/// <summary>
		/// Stopping.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1114Key)]
		Stopping,

		/// <summary>
		/// Starting.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1129Key)]
		Starting,

		/// <summary>
		/// Working.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1130Key)]
		Started,

		/// <summary>
		/// In the process of suspension.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1131Key)]
		Suspending, 

		/// <summary>
		/// Suspended.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Str1132Key)]
		Suspended,
	}

	/// <summary>
	/// Message channel base interface.
	/// </summary>
	public interface IMessageChannel : IDisposable, ICloneable<IMessageChannel>
	{
		/// <summary>
		/// State.
		/// </summary>
		ChannelStates State { get; }

		/// <summary>
		/// <see cref="State"/> change event.
		/// </summary>
		event Action StateChanged;

		/// <summary>
		/// Open channel.
		/// </summary>
		void Open();

		/// <summary>
		/// Close channel.
		/// </summary>
		void Close();

		/// <summary>
		/// Suspend.
		/// </summary>
		void Suspend();

		/// <summary>
		/// Resume.
		/// </summary>
		void Resume();

		/// <summary>
		/// Clear.
		/// </summary>
		void Clear();

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns><see langword="true"/> if the specified message was processed successfully, otherwise, <see langword="false"/>.</returns>
		bool SendInMessage(Message message);

		/// <summary>
		/// New message event.
		/// </summary>
		event Action<Message> NewOutMessage;
	}

	/// <summary>
	/// Message channel, which passes directly to the output all incoming messages.
	/// </summary>
	public class PassThroughMessageChannel : Cloneable<IMessageChannel>, IMessageChannel
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PassThroughMessageChannel"/>.
		/// </summary>
		public PassThroughMessageChannel()
		{
		}

		void IDisposable.Dispose()
		{
		}

		ChannelStates IMessageChannel.State => ChannelStates.Started;

		event Action IMessageChannel.StateChanged
		{
			add { }
			remove { }
		}

		void IMessageChannel.Open()
		{
		}

		void IMessageChannel.Close()
		{
		}

		void IMessageChannel.Suspend()
		{
		}

		void IMessageChannel.Resume()
		{
		}

		void IMessageChannel.Clear()
		{
		}

		bool IMessageChannel.SendInMessage(Message message)
		{
			_newMessage?.Invoke(message);
			return true;
		}

		private Action<Message> _newMessage;

		event Action<Message> IMessageChannel.NewOutMessage
		{
			add => _newMessage += value;
			remove => _newMessage -= value;
		}

		/// <summary>
		/// Create a copy of <see cref="PassThroughMessageChannel"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new PassThroughMessageChannel();
		}
	}
}