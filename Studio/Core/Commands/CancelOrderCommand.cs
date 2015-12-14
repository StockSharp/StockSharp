#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: CancelOrderCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	public class CancelOrderCommand : BaseStudioCommand
	{
		public IEnumerable<Order> Orders { get; private set; }

		public Order Mask { get; private set; }

		public CancelOrderCommand(Order mask)
		{
			if (mask == null)
				throw new ArgumentNullException(nameof(mask));

			Mask = mask;
		}

		public CancelOrderCommand(IEnumerable<Order> orders)
		{
			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			Orders = orders;
		}
	}
}