namespace StockSharp.Transaq.Native
{
	using System;
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

		public ApiClient(Action<string> callback, string dllPath, bool isHft, string path, ApiLogLevels logLevel)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			_callback = callback;

			//esper: если сначала запускаем 32 битную, а потом 64, то коннектор не работает.
			//if (!File.Exists(dllPath))
			//{
			dllPath.CreateDirIfNotExists();
			(Environment.Is64BitProcess ? (isHft ? Resources.txcn64 : Resources.txmlconnector64) : (isHft ? Resources.txcn : Resources.txmlconnector)).Save(dllPath);
			//}

			_api = new Api(dllPath, OnCallback);

			using (var handle = _encoding.ToHGlobal(path))
				CheckErrorResult(_api.Initialize(handle.DangerousGetHandle(), (int)logLevel));
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