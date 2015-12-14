#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: BindCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.BusinessEntities;

	public class BindCommand<T> : BaseStudioCommand
		where T : class
	{
		public T Source { get; private set; }

		public IStudioControl Control { get; private set; }

		public BindCommand(T source, IStudioControl control = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			Source = source;
			Control = control;
		}

		public bool CheckControl(IStudioControl control)
		{
			//если не задан контрол, то считаем, что привязать страегию надо для всех контролов
			return Control == control;
		}
	}

	public class BindStrategyCommand : BindCommand<StrategyContainer>
	{
		public BindStrategyCommand(StrategyContainer source, IStudioControl control = null)
			: base(source, control)
		{
		}
	}

	public class BindConnectorCommand : BindCommand<IConnector>
	{
		public BindConnectorCommand(IConnector source, IStudioControl control = null)
			: base(source, control)
		{
		}
	}
}