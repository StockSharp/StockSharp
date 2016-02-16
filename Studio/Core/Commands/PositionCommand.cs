#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: PositionCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using StockSharp.BusinessEntities;

namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class PositionCommand : BaseStudioCommand
	{
		public PositionCommand(DateTimeOffset time, Position position, bool isNew)
		{
			if (position == null)
				throw new ArgumentNullException("position" +
												"");
			Time = time;
			Position = position;
			IsNew = isNew;
		}

		public DateTimeOffset Time { get; }
		public Position Position { get; }
		public bool IsNew { get; }
	}
}