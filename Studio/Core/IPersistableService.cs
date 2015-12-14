#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.CorePublic
File: IPersistableService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.Community;
	using StockSharp.Xaml.Actipro.Code;

	public interface IPersistableService
	{
		bool ContainsKey(string key);
		TValue GetValue<TValue>(string key, TValue defaultValue = default(TValue));
		void SetValue(string key, object value);
	}

	public static class PersistableServiceHelper
	{
		public static ServerCredentials GetCredentials(this IPersistableService service)
		{
			if (service == null)
				throw new ArgumentNullException(nameof(service));

			return service.GetValue<ServerCredentials>("StockSharpCredentials");
		}

		public static void SetCredentials(this IPersistableService service, ServerCredentials credentials)
		{
			if (service == null)
				throw new ArgumentNullException(nameof(service));

			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			service.SetValue("StockSharpCredentials", credentials);
		}

		public static IEnumerable<CodeReference> GetReferences(this IPersistableService service)
		{
			if (service == null)
				throw new ArgumentNullException(nameof(service));

			return service.GetValue<CodeReference[]>("References");
		}

		public static void SetReferences(this IPersistableService service, IEnumerable<CodeReference> references)
		{
			if (service == null)
				throw new ArgumentNullException(nameof(service));

			if (references == null)
				throw new ArgumentNullException(nameof(references));

			service.SetValue("References", references.ToArray());
		}

		public static SettingsStorage GetStudioSession(this IPersistableService service)
		{
			if (service == null)
				throw new ArgumentNullException(nameof(service));

			return service.GetValue<SettingsStorage>("StudioSession");
		}

		public static void SetStudioSession(this IPersistableService service, SettingsStorage session)
		{
			if (service == null)
				throw new ArgumentNullException(nameof(service));

			if (session == null)
				throw new ArgumentNullException(nameof(session));

			service.SetValue("StudioSession", session);
		}

		public static bool GetAutoConnect(this IPersistableService service)
		{
			if (service == null)
				throw new ArgumentNullException(nameof(service));

			return service.GetValue("AutoConnect", true);
		}

		public static void SetAutoConnect(this IPersistableService service, bool autoConnect)
		{
			if (service == null)
				throw new ArgumentNullException(nameof(service));

			service.SetValue("AutoConnect", autoConnect);
		}
	}
}