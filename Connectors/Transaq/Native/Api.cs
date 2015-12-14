#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Transaq
File: Api.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native
{
	using System;

	using Ecng.Interop;

	using StockSharp.Localization;

	internal class Api : NativeLibrary
	{
		public delegate void CallBack(IntPtr pData);
		private delegate IntPtr InitializeHandler(IntPtr path, int logLevel);
		private readonly InitializeHandler _initialize;
		private delegate IntPtr UninitializeHandler();
		private readonly UninitializeHandler _uninitialize;
		private delegate bool FreeMemoryHandler(IntPtr pData);
		private readonly FreeMemoryHandler _freeMemory;
		private delegate IntPtr SetLogLevelHandler(int logLevel);
		private readonly SetLogLevelHandler _setLogLevel;
		private delegate IntPtr SendCommandHandler(IntPtr pData);
		private readonly SendCommandHandler _sendCommand;
		private delegate bool SetCallbackHandler(CallBack pCallback);
		private readonly SetCallbackHandler _setCallback;

		//public delegate bool SetCallbackExHandler(CallBack pCallback, IntPtr data);
		//private readonly SetCallbackExHandler _setCallbackEx;
		//public delegate void CallBackEx(IntPtr pData, IntPtr userData);

		private readonly CallBack _callback;

		public Api(string dllPath, CallBack callback)
			: base(dllPath)
		{
			_initialize = GetHandler<InitializeHandler>("Initialize");
			_uninitialize = GetHandler<UninitializeHandler>("UnInitialize");
			_freeMemory = GetHandler<FreeMemoryHandler>("FreeMemory");
			_setLogLevel = GetHandler<SetLogLevelHandler>("SetLogLevel");
			_sendCommand = GetHandler<SendCommandHandler>("SendCommand");
			_setCallback = GetHandler<SetCallbackHandler>("SetCallback");
			//_setCallbackEx = GetHandler<SetCallbackExHandler>("SetCallbackEx");

			_callback = callback;
			
			SetCallback(_callback);
			
			// SetCallbackEx(OnCallBackEx, IntPtr.Zero);
		}

		public IntPtr Initialize(IntPtr path, int logLevel)
		{
			ThrowIfDisposed();
			return _initialize(path, logLevel);
		}

		public IntPtr UnInitialize()
		{
			ThrowIfDisposed();
			return _uninitialize();
		}

		public IntPtr SetLogLevel(int logLevel)
		{
			ThrowIfDisposed();
			return _setLogLevel(logLevel);
		}

		public IntPtr SendCommand(IntPtr command)
		{
			ThrowIfDisposed();
			return _sendCommand(command);
		}

		public void FreeMemory(IntPtr ptr)
		{
			_freeMemory(ptr);
		}

		protected override void DisposeNative()
		{
			GC.KeepAlive(_callback);
			base.DisposeNative();
		}

		private void SetCallback(CallBack pCallback)
		{
			ThrowIfDisposed();
			var r = _setCallback(pCallback);

			if (!r)
			{
				throw new ApiException(LocalizedStrings.Str3567);
			}
		}

		//private void SetCallbackEx(CallBack pCallback, IntPtr userData)
		//{
		//	ThrowIfDisposed();
		//	var r = _setCallbackEx(pCallback, userData);

		//	if (!r)
		//	{
		//		throw new ApiException("Не смог установить функцию обратного вызова");
		//	}
		//}
	}
}