namespace StockSharp.Localization;

using System;
using System.Reflection;

using Ecng.Common;

/// <summary>
/// Information for an assembly manifest.
/// </summary>
public static class ProjectDescriptions
{
	static ProjectDescriptions()
	{
		var asm = typeof(ProjectDescriptions).Assembly;

		Company = asm.GetAttribute<AssemblyCompanyAttribute>()?.Company;
		Product = asm.GetAttribute<AssemblyProductAttribute>()?.Product;
		Copyright = asm.GetAttribute<AssemblyCopyrightAttribute>()?.Copyright;
		Trademark = asm.GetAttribute<AssemblyTrademarkAttribute>()?.Trademark;
		Version = asm.GetName().Version;
	}

	/// <summary>
	/// Gets company information.
	/// </summary>
	public static readonly string Company;

	/// <summary>
	/// Gets product information.
	/// </summary>
	public static readonly string Product;

	/// <summary>
	/// Gets copyright information.
	/// </summary>
	public static readonly string Copyright;

	/// <summary>
	/// Gets trademark information.
	/// </summary>
	public static readonly string Trademark;

	/// <summary>
	/// Gets version information.
	/// </summary>
	public static readonly Version Version;
}