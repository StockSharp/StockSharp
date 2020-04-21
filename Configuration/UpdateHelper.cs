namespace StockSharp.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.ComponentModel;

	/// <summary>
	/// App installer client.
	/// </summary>
	public static class UpdateHelper
	{
		private class UpdateException : Exception
		{
			public UpdateCheckStatus Status {get;}
			public UpdateException(UpdateCheckStatus status, string msg = null) : base(msg) => Status = status;
		}

		/// <summary>Update status.</summary>
		public enum UpdateCheckStatus
		{
			/// <summary>Unknown status.</summary>
			Unknown,
			/// <summary>Update in progress.</summary>
			InProgress,
			/// <summary>Updates are available.</summary>
			UpdatesAvailable, 
			/// <summary>No updates available.</summary>
			UpToDate, 
			/// <summary>Installer not found.</summary>
			ErrorInstallerNotFound,
			/// <summary>Upate check error.</summary>
			Error
		}

		private static readonly object _lock = new object();

		private static TaskCompletionSource<string[]> _tcs;

		/// <summary>Check for current application updates.</summary>
		/// <param name="credentials">Server credentials for private repository updates.</param>
		/// <param name="status">Update check status event.</param>
		public static void CheckForUpdates(ServerCredentials credentials, Action<UpdateCheckStatus, string> status)
		{
			if (status is null)
				throw new ArgumentNullException(nameof(status));

			Task.Run(async () =>
			{
				try
				{
					var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
					var lines = await DoCheckForUpdates(credentials, cts.Token, status);
					if(lines.Length == 1 && lines[0].ToLowerInvariant() == "uptodate")
					{
						status.Invoke(UpdateCheckStatus.UpToDate, string.Empty);
						return;
					}

					status.Invoke(UpdateCheckStatus.UpdatesAvailable, lines.Join("\n"));
				}
				catch (UpdateException e)
				{
					status.Invoke(e.Status, e.ToString());
				}
				catch (Exception e)
				{
					status.Invoke(UpdateCheckStatus.Error, e.ToString());
				}
			});
		}

		private static Task<string[]> DoCheckForUpdates(ServerCredentials credentials, CancellationToken token, Action<UpdateCheckStatus, string> status)
		{
			lock (_lock)
			{
				var tcs = _tcs;
				if(tcs != null)
					return tcs.Task;

				_tcs = new TaskCompletionSource<string[]>();
			}

			var task = _tcs.Task;

			_tcs.Task.ContinueWith(t => _tcs = null);

			try
			{
				var utility = InstallerLocator.Instance.GetCheckUpdatesPath();

				if (utility.IsEmptyOrWhiteSpace())
				{
					_tcs.SetException(new UpdateException(UpdateCheckStatus.ErrorInstallerNotFound));
					return task;
				}

				status.Invoke(UpdateCheckStatus.InProgress, string.Empty);

				var asm = Assembly.GetEntryAssembly();
				var appPath = Path.GetDirectoryName(asm.Location);

				var procInfo = new ProcessStartInfo(utility, $"\"{appPath}\" \"{credentials.Email}\" \"{credentials.Password}\"")
				{
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden
				};

				var proc = new Process {EnableRaisingEvents = true, StartInfo = procInfo};
				var data = new List<string>();
				var errors = new List<string>();

				proc.OutputDataReceived += (a, e) =>
				{
					if (!e.Data.IsEmptyOrWhiteSpace())
						data.Add(e.Data.Trim());
				};
				proc.ErrorDataReceived += (a, e) =>
				{
					if (!e.Data.IsEmptyOrWhiteSpace())
						errors.Add(e.Data);
				};
				proc.Exited += (sender, args) =>
				{
					if (proc.ExitCode == 0 && !errors.Any())
					{
						if (data.Count > 0)
							_tcs.SetResult(data.ToArray());
						else
							_tcs.SetException(new Exception("update check returned no data"));
					}
					else
					{
						_tcs.SetException(new Exception($"update check errors:\n{errors.Join("\n")}"));
					}
				};

				token.Register(() =>
				{
					if (proc.HasExited)
						return;

					errors.Add("process was killed");
					try
					{
						proc.Kill();
					}
					catch (Exception e)
					{
						errors.Add(e.ToString());
					}
				}, true);

				proc.Start();
				proc.BeginOutputReadLine();
				proc.BeginErrorReadLine();
			}
			catch (Exception e)
			{
				_tcs?.SetException(e);
			}

			return task;
		}
	}
}
