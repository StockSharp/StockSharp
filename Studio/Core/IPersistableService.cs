namespace StockSharp.Studio.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.Community;
	using StockSharp.Xaml.Code;

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
				throw new ArgumentNullException("service");

			return service.GetValue<ServerCredentials>("StockSharpCredentials");
		}

		public static void SetCredentials(this IPersistableService service, ServerCredentials credentials)
		{
			if (service == null)
				throw new ArgumentNullException("service");

			if (credentials == null)
				throw new ArgumentNullException("credentials");

			service.SetValue("StockSharpCredentials", credentials);
		}

		public static IEnumerable<CodeReference> GetReferences(this IPersistableService service)
		{
			if (service == null)
				throw new ArgumentNullException("service");

			return service.GetValue<CodeReference[]>("References");
		}

		public static void SetReferences(this IPersistableService service, IEnumerable<CodeReference> references)
		{
			if (service == null)
				throw new ArgumentNullException("service");

			if (references == null)
				throw new ArgumentNullException("references");

			service.SetValue("References", references.ToArray());
		}

		public static SettingsStorage GetStudioSession(this IPersistableService service)
		{
			if (service == null)
				throw new ArgumentNullException("service");

			return service.GetValue<SettingsStorage>("StudioSession");
		}

		public static void SetStudioSession(this IPersistableService service, SettingsStorage session)
		{
			if (service == null)
				throw new ArgumentNullException("service");

			if (session == null)
				throw new ArgumentNullException("session");

			service.SetValue("StudioSession", session);
		}

		public static bool GetAutoConnect(this IPersistableService service)
		{
			if (service == null)
				throw new ArgumentNullException("service");

			return service.GetValue("AutoConnect", true);
		}

		public static void SetAutoConnect(this IPersistableService service, bool autoConnect)
		{
			if (service == null)
				throw new ArgumentNullException("service");

			service.SetValue("AutoConnect", autoConnect);
		}
	}
}