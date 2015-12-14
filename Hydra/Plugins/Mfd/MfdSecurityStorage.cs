#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Mfd.MfdPublic
File: MfdSecurityStorage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Mfd
{
	using System;

	using Ecng.Collections;

	using StockSharp.Algo.History.Russian;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Core;

	class MfdSecurityStorage : NativeIdSecurityStorage<string>
	{
		public MfdSecurityStorage(IEntityRegistry entityRegistry)
			: base(entityRegistry, StringComparer.InvariantCultureIgnoreCase)
		{
		}

		protected override string CreateNativeId(Security security)
		{
			return (string)security.ExtensionInfo.TryGetValue(MfdHistorySource.SecurityIdField);
		}
	}
}