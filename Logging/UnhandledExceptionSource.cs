namespace StockSharp.Logging
{
	using System;
	using System.Threading.Tasks;

	using Ecng.Common;

	/// <summary>
	/// Источник логов, отсылающий информацию о необработанных ошибках <see cref="AppDomain.UnhandledException"/> и <see cref="TaskScheduler.UnobservedTaskException"/>.
	/// </summary>
	public class UnhandledExceptionSource : BaseLogSource
	{
		/// <summary>
		/// Создать <see cref="UnhandledExceptionSource"/>.
		/// </summary>
		public UnhandledExceptionSource()
		{
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
			TaskScheduler.UnobservedTaskException += OnTaskSchedulerException;
		}

		/// <summary>
		/// Название.
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
			RaiseLog(new LogMessage(this, TimeHelper.Now, LogLevels.Error, () => e.ExceptionObject.ToString()));
		}

		private void OnTaskSchedulerException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			RaiseLog(new LogMessage(this, TimeHelper.Now, LogLevels.Error, () => e.Exception.ToString()));
			e.SetObserved();
		}

		/// <summary>
		/// Удалить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
			TaskScheduler.UnobservedTaskException -= OnTaskSchedulerException;
			base.DisposeManaged();
		}
	}
}