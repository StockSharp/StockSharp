#region Using Directives

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

using StockSharp.Localization;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyCompany(ProjectDescriptions.Company)]
[assembly: AssemblyProduct(ProjectDescriptions.Product)]
[assembly: AssemblyCopyright(ProjectDescriptions.Copyright)]
[assembly: AssemblyTrademark(ProjectDescriptions.Trademark)]
[assembly: AssemblyCulture("")]
#if DEBUG

[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

//[assembly: AllowPartiallyTrustedCallers]

[assembly: CLSCompliant(true)]
[assembly: NeutralResourcesLanguage("en-US")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion(ProjectDescriptions.Version)]
[assembly: AssemblyFileVersion(ProjectDescriptions.Version)]