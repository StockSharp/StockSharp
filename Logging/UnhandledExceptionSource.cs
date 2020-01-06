#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: UnhandledExceptionSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;
	using System.Threading.Tasks;

	using Ecng.Common;

	/// <summary>
	/// The logs source sending information about unhandled errors <see cref="AppDomain.UnhandledException"/> and <see cref="TaskScheduler.UnobservedTaskException"/>.
	/// </summary>
	public class UnhandledExceptionSource : BaseLogSource
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UnhandledExceptionSource"/>.
		/// </summary>
		public UnhandledExceptionSource()
		{
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			TaskScheduler.UnobservedTaskException += OnTaskSchedulerException;
		}

		/// <inheritdoc />
		public override string Name => "Unhandled Exception";

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			RaiseLog(new LogMessage(this, TimeHelper.NowWithOffset, LogLevels.Error, () => e.ExceptionObject.ToString()));
		}

		private void OnTaskSchedulerException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			RaiseLog(new LogMessage(this, TimeHelper.NowWithOffset, LogLevels.Error, () => e.Exception.ToString()));
			e.SetObserved();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
			TaskScheduler.UnobservedTaskException -= OnTaskSchedulerException;
			base.DisposeManaged();
		}
	}
}