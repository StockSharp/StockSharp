using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("S#.Xaml")]
[assembly: AssemblyDescription("Misc graphical components.")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("3B91EF1C-89BB-4BC0-8D4B-640A738253ED")]

//In order to begin building localizable applications, set 
//<UICulture>CultureYouAreCodingWith</UICulture> in your .csproj file
//inside a <PropertyGroup>.  For example, if you are using US english
//in your source files, set the <UICulture> to en-US.  Then uncomment
//the NeutralResourceLanguage attribute below.  Update the "en-US" in
//the line below to match the UICulture setting in the project file.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
	//(used if a resource is not found in the page, 
	// or application resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
	//(used if a resource is not found in the page, 
	// app, or any theme specific resource dictionaries)
)]

[assembly: XmlnsDefinition("http://schemas.stocksharp.com/xaml", "StockSharp.Xaml")]
[assembly: XmlnsDefinition("http://schemas.stocksharp.com/xaml", "StockSharp.Xaml.PropertyGrid")]