#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Transaq
File: ApiClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native
{
	using System;
	using System.IO;
	using System.Text;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;

	using StockSharp.Transaq.Properties;

	internal class ApiClient : Disposable
	{
		private readonly Api _api;
		private readonly Action<string> _callback;
		private static readonly Encoding _encoding = Encoding.UTF8;

		public ApiClient(Action<string> callback, string dllPath, bool overrideDll, bool isHft, string logsPath, ApiLogLevels logLevel)
		{
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));

			_callback = callback;

			if (overrideDll || !File.Exists(dllPath))
			{
				dllPath.CreateDirIfNotExists();
				(Environment.Is64BitProcess ? (isHft ? Resources.txcn64 : Resources.txmlconnector64) : (isHft ? Resources.txcn : Resources.txmlconnector)).Save(dllPath);
			}

			_api = new Api(dllPath, OnCallback);

			try
			{
				Directory.CreateDirectory(logsPath);

				using (var handle = _encoding.ToHGlobal(logsPath + "\0"))
					CheckErrorResult(_api.Initialize(handle.DangerousGetHandle(), (int)logLevel));
			}
			catch (Exception)
			{
				_api.Dispose();
				throw;
			}
		}

		public string SendCommand(string command)
		{
			using (var handle = _encoding.ToHGlobal(command))
			{
				var pResult = _api.SendCommand(handle.DangerousGetHandle());
				return ProcessPtrResult(pResult);
			}
		}

		public void SetLogLevel(ApiLogLevels logLevel)
		{
			CheckErrorResult(_api.SetLogLevel((int)logLevel));
		}

		private void OnCallback(IntPtr pData)
		{
			_callback(ProcessPtrResult(pData));
		}

		private string ProcessPtrResult(IntPtr pResult)
		{
			if (pResult != IntPtr.Zero)
			{
				var result = _encoding.ToString(pResult).Replace("&", "&amp;");
				_api.FreeMemory(pResult);
				return result;
			}

			return string.Empty;
		}

		private void CheckErrorResult(IntPtr ptr)
		{
			var errorMessage = ProcessPtrResult(ptr);

			if (!errorMessage.IsEmpty())
				throw new ApiException(errorMessage);
		}

		protected override void DisposeManaged()
		{
			CheckErrorResult(_api.UnInitialize());
			_api.Dispose();

			base.DisposeManaged();
		}
	}
}