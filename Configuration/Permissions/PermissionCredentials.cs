namespace StockSharp.Configuration.Permissions;

using System.Net;

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
	}

	private IEnumerable<IPAddress> _ipRestrictions = [];

	/// <summary>
	/// IP address restrictions.
	/// </summary>
	public IEnumerable<IPAddress> IpRestrictions
	{
		get => _ipRestrictions;
		set
		{
			_ipRestrictions = value ?? throw new ArgumentNullException(nameof(value));
			NotifyChanged(nameof(IpRestrictions));
		}
	}

	/// <summary>
	/// Permission set.
	/// </summary>
	public SynchronizedDictionary<UserPermissions, SynchronizedDictionary<(string name, string param, string extra, DateTime? till), bool>> Permissions { get; } = [];

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(IpRestrictions), IpRestrictions.Select(e => e.To<string>()).JoinComma());

		using (Permissions.EnterScope())
		{
			storage.SetValue(nameof(Permissions), Permissions
				.Select(p =>
					new SettingsStorage()
						.Set("Permission", p.Key)
						.Set("Settings", p.Value
							.Select(p1 =>
								new SettingsStorage()
									.Set("Name", p1.Key.name)
									.Set("Param", p1.Key.param)
									.Set("Extra", p1.Key.extra)
									.Set("Till", p1.Key.till)
									.Set("IsEnabled", p1.Value)
							).ToArray()
						)
				).ToArray());
		}
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		IpRestrictions = [.. storage.GetValue<string>(nameof(IpRestrictions)).SplitByComma().Select(s => s.To<IPAddress>())];

		var permissions = storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Permissions));

		using (Permissions.EnterScope())
		{
			Permissions.Clear();

			if (permissions == null)
				return;

			foreach (var permission in permissions)
			{
				Permissions
					.SafeAdd(permission.GetValue<UserPermissions>("Permission"))
					.AddRange(permission
						.GetValue<IEnumerable<SettingsStorage>>("Settings")
						.Select(s => new KeyValuePair<(string, string, string, DateTime?), bool>(
							(
								s.GetValue<string>("Name"),
								s.GetValue<string>("Param"),
								s.GetValue<string>("Extra"),
								s.GetValue<DateTime?>("Till")
							),
							s.GetValue<bool>("IsEnabled"))));
			}
		}
	}
}