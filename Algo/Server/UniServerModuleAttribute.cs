namespace StockSharp.Algo.Server
{
	using System;

	/// <summary>
	/// Attribute that identifies a class as UniServer module.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class UniServerModuleAttribute : Attribute
	{
		/// <summary>
		/// Construct instance of <see cref="UniServerModuleAttribute"/>.
		/// </summary>
		/// <param name="name">Name of the module.</param>
		/// <param name="ver">Version of the module.</param>
		public UniServerModuleAttribute(string name, string ver)
		{
			Name = name;
			Version = new Version(ver);
		}

		/// <summary>
		/// Name of the module. This name is displayed in admin utility.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Version of the module.
		/// </summary>
		public Version Version { get; set; }
	}
}
