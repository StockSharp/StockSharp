#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.IQFeed.IQFeed
File: IQFeedMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.IQFeed
{
	using System;

	using StockSharp.Messages;

	static class ExtendedMessageTypes
	{
		public const MessageTypes End = (MessageTypes)(-1000);
		public const MessageTypes System = (MessageTypes)(-1001);
		public const MessageTypes ListedMarket = (MessageTypes)(-1002);
		public const MessageTypes SecurityType = (MessageTypes)(-1003);
		public const MessageTypes Data = (MessageTypes)(-1004);
		public const MessageTypes HistoryExtraDayCandle = (MessageTypes)(-1005);
		public const MessageTypes Fundamental = (MessageTypes)(-1006);
		public const MessageTypes NewsStory = (MessageTypes)(-1007);
	}

	internal enum IQFeedSearchField
	{
		Symbol, 
		Description
	}

	internal enum IQFeedFilterType
	{
		Market,
		SecurityType
	}

	internal class IQFeedSystemMessage : Message
	{
		public IQFeedWrapper Feed { get; private set; }
		public string Value { get; private set; }

		public IQFeedSystemMessage(IQFeedWrapper feed, string value)
			: base(ExtendedMessageTypes.System)
		{
			if (feed == null)
				throw new ArgumentNullException(nameof(feed));

			Feed = feed;
			Value = value;
		}
	}

	internal class IQFeedEndMessage : Message
	{
		public IQFeedEndMessage()
			: base(ExtendedMessageTypes.End)
		{
		}
	}

	internal class IQFeedSecurityTypeMessage : Message
	{
		public IQFeedSecurityTypeMessage(int id, string code, string name)
			: base(ExtendedMessageTypes.SecurityType)
		{
			Name = name;
			Code = code;
			Id = id;
		}

		public int Id { get; private set; }
		public string Code { get; private set; }
		public string Name { get; private set; }
	}

	internal class IQFeedListedMarketMessage : Message
	{
		public IQFeedListedMarketMessage(int id, string code, string name)
			: base(ExtendedMessageTypes.ListedMarket)
		{
			Name = name;
			Code = code;
			Id = id;
		}

		public int Id { get; private set; }
		public string Code { get; private set; }
		public string Name { get; private set; }
	}

	internal class IQFeedDataMessage : Message
	{
		public string Value { get; private set; }

		public IQFeedDataMessage(string value)
			: base(ExtendedMessageTypes.Data)
		{
			Value = value;
		}
	}
}