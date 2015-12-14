#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: %Namespace%.Xaml.ActiproPublic
File: AssemblyInfo.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("S#.Xaml.Actipro")]
[assembly: AssemblyDescription("Actipro graphical components.")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("3baaf5d9-cbd4-4c1f-876e-cb76952e8621")]

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
	//(used if a resource is not found in the page, 
	// or application resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
	//(used if a resource is not found in the page, 
	// app, or any theme specific resource dictionaries)
)]

[assembly: XmlnsDefinition("http://schemas.stocksharp.com/xaml", "StockSharp.Xaml.Actipro")]
[assembly: XmlnsDefinition("http://schemas.stocksharp.com/xaml", "StockSharp.Xaml.Actipro.Code")]