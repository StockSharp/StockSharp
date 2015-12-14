#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.SmartCom.SmartCom
File: SmartComService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.SmartCom
{
	using System;
	using System.Diagnostics;
	using System.Linq;
	using System.ServiceProcess;

	using Ecng.Common;

	/// <summary>
	/// Вспомогательный класс управления сервисом SmartCOM 2.
	/// </summary>
	public static class SmartComService
	{
		/// <summary>
		/// Остановить процесс SmartCom2.exe.
		/// </summary>
		public static void KillSmartComProcess()
		{
			var process = Process.GetProcesses().FirstOrDefault(p => p.ProcessName == "SmartCom2");

			if (process == null)
				return;

			process.Kill();
			TimeSpan.FromSeconds(3).Sleep();
		}

		/// <summary>
		/// Перезапустить службу SmartCOM.
		/// </summary>
		/// <param name="timeout">Ограничение по времени для перезапуска службы SmartCOM.</param>
		public static void RestartSmartComService(TimeSpan timeout)
		{
			var service = new ServiceController("SmartCom2");

			var msStarting = Environment.TickCount;
			var waitIndefinitely = timeout == TimeSpan.Zero;

			if (service.CanStop)
			{
				//this.AddDebugLog(LocalizedStrings.Str1891);
				service.Stop();
			}

			if (waitIndefinitely)
				service.WaitForStatus(ServiceControllerStatus.Stopped);
			else
				service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

			//this.AddDebugLog(LocalizedStrings.Str1892);

			var msStarted = Environment.TickCount;
			timeout = timeout - TimeSpan.FromMilliseconds((msStarted - msStarting));

			//this.AddDebugLog(LocalizedStrings.Str1893);

			service.Start();

			if (waitIndefinitely)
				service.WaitForStatus(ServiceControllerStatus.Running);
			else
				service.WaitForStatus(ServiceControllerStatus.Running, timeout);

			//this.AddDebugLog(LocalizedStrings.Str1894);
		}
	}
}
