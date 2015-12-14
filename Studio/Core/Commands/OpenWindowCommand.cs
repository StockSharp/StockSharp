#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: OpenWindowCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

	public class OpenWindowCommand : BaseStudioCommand
	{
		public OpenWindowCommand(string id, Type ctrlType, bool isToolWindow, object context = null)
		{
			if (ctrlType == null)
				throw new ArgumentNullException(nameof(ctrlType));

			Id = id;
			CtrlType = ctrlType;
			IsToolWindow = isToolWindow;
			Context = context;
		}

		public string Id { get; private set; }
		public Type CtrlType { get; private set; }
		public bool IsToolWindow { get; private set; }
		public object Context { get; private set; }
	}
}