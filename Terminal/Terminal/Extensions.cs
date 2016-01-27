#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.SampleDiagramPublic
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Terminal
{
	using System;

	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Logging;

	static class Extensions
	{
		public static void DoIfElse<T>(this object value, Action<T> action, Action elseAction)
			where T : class
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			if (elseAction == null)
				throw new ArgumentNullException(nameof(elseAction));

			var typedValue = value as T;

			if (typedValue != null)
			{
				action(typedValue);
			}
			else
				elseAction();
		}

		public static void TryLoadSettings<T>(this SettingsStorage storage, string name, Action<T> load)
		{
			try
			{
				var settings = storage.GetValue<T>(name);

				if (settings == null)
					return;

				load(settings);
			}
			catch (Exception excp)
			{
				ConfigManager.GetService<LogManager>().Application.AddErrorLog(excp);
			}
		}
	}
}