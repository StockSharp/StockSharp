# StockSharp Algo.Compilation

**Algo.Compilation** provides runtime code compilation services for the StockSharp trading platform. It enables you to compile trading algorithms and analytics scripts written in different languages, including C#, Visual Basic, F#, and Python, directly from your application.

## Features

- **Multi-language support** – Uses Roslyn for C# and Visual Basic, the F# compiler for F# scripts, and IronPython for Python integration.
- **Dynamic script execution** – Compile and execute code snippets or whole modules at runtime. This allows building strategies and analytics that can be modified without recompiling the entire application.
- **Python utilities** – On initialization, common Python helper scripts are extracted to `Paths.PythonUtilsPath` so they can be imported from your own Python code.
- **Compiler registry** – Registers available compilers with `ConfigManager` so that other StockSharp components can obtain the correct compiler based on file extension.
- **Custom type descriptors for Python objects** – Allows Python classes to expose properties in .NET components such as property grids.
- **Caching support** – Integrates with `CompilerCache` to reuse previously built assemblies when possible.

## Initialization

Before compiling any code you must call `CompilationExtensions.Init` once during application startup:

```csharp
await CompilationExtensions.Init(logs, extraPythonCommon, cancellationToken);
```

- `logs` – implementation of `ILogReceiver` that collects output from the compilers and the Python engine.
- `extraPythonCommon` – optional additional Python files to copy into the utilities folder.
- `cancellationToken` – allows cancelling the initialization.

This method configures the IronPython engine, writes common Python scripts to `Paths.PythonUtilsPath`, and registers the compilers.

## Compiling Code

The `CodeInfo` class represents a source file and contains helper methods to compile it. The basic workflow is:

```csharp
var code = new CodeInfo
{
    Name = "MyScript",
    Text = sourceCode,           // your script text
    Language = FileExts.CSharp,  // or FileExts.FSharp, FileExts.Python, ...
};

var errors = await code.CompileAsync(type => typeof(IMyInterface).IsAssignableFrom(type), null, CancellationToken.None);

if (!errors.Any())
{
    var instance = Activator.CreateInstance(code.ObjectType!);
    // use the compiled object
}
```

`CodeInfo` manages assembly, project, and NuGet references. It also caches compiled assemblies when the compiler supports it.



