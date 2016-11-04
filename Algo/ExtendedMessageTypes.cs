#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: ExtendedMessageTypes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System.Reflection;

	using StockSharp.Messages;

	[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	static class ExtendedMessageTypes
	{
		public const MessageTypes Last = (MessageTypes)(-1);
		public const MessageTypes Clearing = (MessageTypes)(-2);
		public const MessageTypes EmulationState = (MessageTypes)(-5);
		public const MessageTypes Generator = (MessageTypes)(-6);
		public const MessageTypes CommissionRule = (MessageTypes)(-7);
		public const MessageTypes HistorySource = (MessageTypes)(-8);
		public const MessageTypes RemoveSecurity = (MessageTypes)(-9);
		public const MessageTypes HistoryInitialized = (MessageTypes)(-10);
	}
}