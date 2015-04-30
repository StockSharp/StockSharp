namespace StockSharp.IQFeed.Metadata
{
	using System;

	internal class IQFeedMessageField
	{
		public IQFeedMessageField(IQFeedMessageType messageType, string name, Type type)
		{
			MessageType = messageType;
			Name = name;
			Type = type;
		}

		public IQFeedMessageType MessageType { get; set; }

		public string Name { get; set; }

		public Type Type { get; set; }
	}
}
