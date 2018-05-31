namespace StockSharp.Algo
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Community;
	using Pair = System.Tuple<string, string, object, System.DateTime?>;

	/// <summary>
	/// Credentials with set of permissions.
	/// </summary>
	public class PermissionCredentials : ServerCredentials
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionCredentials"/>.
		/// </summary>
		public PermissionCredentials()
		{
			Permissions.SafeAdd(UserPermissions.Load);
			Permissions.SafeAdd(UserPermissions.SecurityLookup);
			Permissions.SafeAdd(UserPermissions.ExchangeLookup);
			Permissions.SafeAdd(UserPermissions.ExchangeBoardLookup);
		}

		/// <summary>
		/// 
		/// </summary>
		public string IpRestrictionsStr
		{
			get
			{
				lock (IpRestrictions.SyncRoot)
					return IpRestrictions.Select(e => e.To<string>()).Join(",");
			}
			set
			{
				lock (IpRestrictions.SyncRoot)
				{
					IpRestrictions.Clear();

					var ipRestrictions = value;

					if (ipRestrictions != null)
					{
						IpRestrictions.AddRange(ipRestrictions.Split(",").Select(s => s.To<IPAddress>()));
					}
				}
			}
		}

		/// <summary>
		/// IP address restrictions.
		/// </summary>
		public CachedSynchronizedSet<IPAddress> IpRestrictions { get; } = new CachedSynchronizedSet<IPAddress>();

		/// <summary>
		/// Permission set.
		/// </summary>
		public SynchronizedDictionary<UserPermissions, SynchronizedDictionary<Pair, bool>> Permissions { get; } = new SynchronizedDictionary<UserPermissions, SynchronizedDictionary<Pair, bool>>();

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(IpRestrictions), IpRestrictionsStr);

			lock (Permissions.SyncRoot)
				storage.SetValue(nameof(Permissions), Permissions.ToDictionary(p => p.Key, p => (IDictionary<Pair, bool>)p.Value.ToDictionary(p1 => p1.Key, p1 => p1.Value)));
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			IpRestrictionsStr = storage.GetValue<string>(nameof(IpRestrictions));

			var dict = storage.GetValue<IDictionary<UserPermissions, IDictionary<Pair, bool>>>(nameof(Permissions));

			lock (Permissions.SyncRoot)
			{
				Permissions.Clear();

				if (dict == null)
					return;

				foreach (var pair in dict)
				{
					Permissions.SafeAdd(pair.Key).AddRange(pair.Value);
				}
			}
		}
	}
}