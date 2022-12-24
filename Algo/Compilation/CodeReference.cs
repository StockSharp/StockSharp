namespace StockSharp.Algo.Compilation
{
	using System.IO;

	using Ecng.Common;
	using Ecng.Compilation;
	using Ecng.Serialization;

	/// <summary>
	/// The reference to the .NET assembly.
	/// </summary>
	public class CodeReference : IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CodeReference"/>.
		/// </summary>
		public CodeReference()
		{
		}

		/// <summary>
		/// The assembly name.
		/// </summary>
		public string Name => Path.GetFileNameWithoutExtension(Location);

		/// <summary>
		/// The path to the assembly.
		/// </summary>
		public string Location { get; set; }

		/// <summary>
		/// <see cref="Location"/>.
		/// </summary>
		public string FullLocation
		{
			get
			{
				var location = Location;

				if (File.Exists(location))
					return location;

				var fileName = Path.GetFileName(Location);

				if (location.EqualsIgnoreCase(fileName))
				{
					var tmp = Path.Combine(Directory.GetCurrentDirectory(), fileName);

					if (File.Exists(tmp))
						return tmp;

					tmp = Path.Combine(ICompilerExtensions.RuntimePath, fileName);

					if (File.Exists(tmp))
						return tmp;
				}

				return location;
			}
		}

		/// <summary>
		/// Is valid.
		/// </summary>
		public bool IsValid => File.Exists(FullLocation);

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			Location = storage.GetValue<string>(nameof(Location));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Location), Location);
		}

		/// <inheritdoc />
		public override string ToString() => FullLocation;
	}
}