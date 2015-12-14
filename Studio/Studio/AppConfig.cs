#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.StudioPublic
File: AppConfig.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Logging;
	using StockSharp.Studio.Configuration;

	public class AppConfig
	{
		private readonly CachedSynchronizedList<Type> _toolControls = new CachedSynchronizedList<Type>();
		private readonly CachedSynchronizedList<Type> _strategyControls = new CachedSynchronizedList<Type>();

		private static AppConfig _instance;

		public static AppConfig Instance
		{
			get { return _instance ?? (_instance = new AppConfig()); }
		}

		private AppConfig()
		{
			var section = ConfigManager.GetSection<StudioSection>();

			FixServerAddresss = section.FixServerAddress;

			SafeAdd<ControlElement>(section.ToolControls, elem => _toolControls.Add(elem.Type.To<Type>()));
			SafeAdd<ControlElement>(section.StrategyControls, elem => _strategyControls.Add(elem.Type.To<Type>()));
		}

		private static void SafeAdd<T1>(IEnumerable from, Action<T1> action)
		{
			foreach (T1 item in from)
			{
				try
				{
					action(item);
				}
				catch (Exception e)
				{
					e.LogError();
				}
			}
		}

		public string FixServerAddresss { get; private set; }

		public IEnumerable<Type> ToolControls
		{
			get { return _toolControls.Cache; }
		}

		public IEnumerable<Type> StrategyControls
		{
			get { return _strategyControls.Cache; }
		}
	}
}
