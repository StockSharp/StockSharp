#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: ErrorLoggingAttribute.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.ServiceModel;
	using System.ServiceModel.Channels;
	using System.ServiceModel.Description;
	using System.ServiceModel.Dispatcher;

	/// <summary>
	/// The attribute for the WCF server that automatically records all errors to <see cref="LoggingHelper.LogError"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class ErrorLoggingAttribute : Attribute, IServiceBehavior
	{
		private sealed class ErrorHandler : IErrorHandler
		{
			private ErrorHandler()
			{
			}

			private static readonly Lazy<ErrorHandler> _instance = new Lazy<ErrorHandler>(() => new ErrorHandler());

			public static ErrorHandler Instance => _instance.Value;

			public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
			{
			}

			public bool HandleError(Exception error)
			{
				error.LogError();
				return true;
			}
		}

		void IServiceBehavior.Validate(ServiceDescription description, ServiceHostBase serviceHostBase)
		{
		}

		void IServiceBehavior.AddBindingParameters(ServiceDescription description, ServiceHostBase serviceHostBase,
												   Collection<ServiceEndpoint> endpoints,
												   BindingParameterCollection parameters)
		{
		}

		void IServiceBehavior.ApplyDispatchBehavior(ServiceDescription description, ServiceHostBase serviceHostBase)
		{
			foreach (var channelDispatcher in serviceHostBase.ChannelDispatchers.Cast<ChannelDispatcher>())
				channelDispatcher.ErrorHandlers.Add(ErrorHandler.Instance);
		}
	}
}