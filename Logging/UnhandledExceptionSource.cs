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

		/// <summary>
		/// Name.
		/// </summary>
		public override string Name
		{
			get
			{
				return "Unhandled Exception";
			}
		}

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