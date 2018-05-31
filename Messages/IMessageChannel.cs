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

	using Ecng.Common;

	/// <summary>
	/// Message sender base interface.
	/// </summary>
	public interface IMessageSender
	{
		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		void SendInMessage(Message message);

		/// <summary>
		/// Send outgoing message.
		/// </summary>
		/// <param name="message">Message.</param>
		void SendOutMessage(Message message);
	}

	/// <summary>
	/// Message channel base interface.
	/// </summary>
	public interface IMessageChannel : IDisposable, ICloneable<IMessageChannel>
	{
		/// <summary>
		/// Is channel opened.
		/// </summary>
		bool IsOpened { get; }

		/// <summary>
		/// Open channel.
		/// </summary>
		void Open();

		/// <summary>
		/// Close channel.
		/// </summary>
		void Close();

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		void SendInMessage(Message message);

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

		bool IMessageChannel.IsOpened => true;

		void IMessageChannel.Open()
		{
		}

		void IMessageChannel.Close()
		{
		}

		void IMessageChannel.SendInMessage(Message message)
		{
			_newMessage?.Invoke(message);
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