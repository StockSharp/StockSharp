#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: ClosePositionCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class ClosePositionCommand : BaseStudioCommand
	{
		public Position Position { get; private set; }
		public Security Security {get;}

		public ClosePositionCommand(Security sec)
		{
			Security = sec;
		}

		public ClosePositionCommand(Position position)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			Position = position;
		}
	}
}