#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Core.CorePublic
File: HydraTaskSettingsList.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Core
{
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;

	/// <summary>
	/// Коллекция настроек для <see cref="IHydraTask"/>.
	/// </summary>
	public class HydraTaskSettingsList : BaseStorageEntityList<HydraTaskSettings>
	{
		/// <summary>
		/// Создать <see cref="HydraTaskSettingsList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public HydraTaskSettingsList(IStorage storage)
			: base(storage)
		{
		}
	}
}