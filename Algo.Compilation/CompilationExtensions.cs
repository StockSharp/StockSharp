namespace StockSharp.Algo.Compilation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Configuration;
using Ecng.Reflection;
using Ecng.Logging;
using Ecng.ComponentModel;
using Ecng.Compilation;
using Ecng.Compilation.FSharp;
using Ecng.Compilation.Python;
using Ecng.Compilation.Roslyn;

using IronPython.Hosting;

using Microsoft.Scripting.Utils;

using StockSharp.Configuration;

/// <summary>
/// Provides extension methods and helpers for dynamic compilation and Python integration in StockSharp Studio.
/// </summary>
public static class CompilationExtensions
{
	private class PythonStream(ILogReceiver logs) : Stream
	{
		private readonly ILogReceiver _logs = logs ?? throw new ArgumentNullException(nameof(logs));

		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => true;

		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override void Write(byte[] buffer, int offset, int count)
		{
			_logs.LogInfo(buffer.UTF8(offset, count));
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
	}

	private static IEnumerable<(string name, string body)> GetPythonCommon()
	{
		var assembly = typeof(CompilationExtensions).Assembly;

		var resourceNames = assembly.GetManifestResourceNames();

		var prefix = $"{typeof(CompilationExtensions).Namespace}.python_common.";

		foreach (var resourceName in resourceNames.Where(n => n.StartsWithIgnoreCase(prefix)))
		{
			using var stream = assembly.GetManifestResourceStream(resourceName)
				?? throw new InvalidOperationException(resourceName);

			using var reader = new StreamReader(stream);
			yield return (resourceName.Remove(prefix, true), reader.ReadToEnd());
		}
	}

	private class PythonCustomTypeDescriptorProvider : ICustomTypeDescriptorProvider
	{
		private class PythonCustomTypeDescriptor(Type type, object instance) : ICustomTypeDescriptorEx
		{
			private class PythonPropertyDescriptor(PropertyInfo propInfo, Type componentType, object instance) : NamedPropertyDescriptor(propInfo.Name, [.. propInfo.GetAttributes()])
			{
				public override Type ComponentType { get; } = componentType ?? throw new ArgumentNullException(nameof(componentType));
				public override bool IsReadOnly { get; } = !propInfo.IsModifiable();
				public override Type PropertyType { get; } = propInfo.PropertyType;

				public override bool CanResetValue(object component) => false;
				public override object GetValue(object component) => propInfo.GetValue(instance);
				public override void SetValue(object component, object value) => propInfo.SetValue(instance, value);
				public override void ResetValue(object component) => throw new NotSupportedException();
				public override bool ShouldSerializeValue(object component) => false;
			}

			private readonly ICustomTypeDescriptor _underlying = (ICustomTypeDescriptor)instance;
			private readonly PropertyDescriptorCollection _props = new([.. type.GetProperties().Select(p => new PythonPropertyDescriptor(p, type, instance))]);

			public object Instance { get; } = instance ?? throw new ArgumentNullException(nameof(instance));

			AttributeCollection ICustomTypeDescriptor.GetAttributes() => _underlying.GetAttributes();
			string ICustomTypeDescriptor.GetClassName() => _underlying.GetClassName();
			string ICustomTypeDescriptor.GetComponentName() => _underlying.GetComponentName();
			TypeConverter ICustomTypeDescriptor.GetConverter() => _underlying.GetConverter();
			object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => _underlying.GetEditor(editorBaseType);

			EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => _underlying.GetDefaultEvent();
			EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => _underlying.GetEvents();
			EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => _underlying.GetEvents(attributes);

			PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => GetProperties().TryGetDefault(type);
			public PropertyDescriptorCollection GetProperties() => _props;
			PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => this.GetFilteredProperties(attributes);
			object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => Instance;
		}

		bool ICustomTypeDescriptorProvider.TryGet(Type type, object instance, out ICustomTypeDescriptor descriptor)
		{
			if (type is null)		throw new ArgumentNullException(nameof(type));
			if (instance is null)	throw new ArgumentNullException(nameof(instance));

			descriptor = default;

			if (!instance.IsPythonObject())
				return false;

			descriptor = new PythonCustomTypeDescriptor(type, instance);
			return true;
		}
	}

	/// <summary>
	/// Initializes the compilation environment, including Python engine setup, resource extraction, and compiler registration.
	/// </summary>
	/// <param name="logs">The log receiver for output and error messages.</param>
	/// <param name="extraPythonCommon">Additional Python common files to include.</param>
	/// <param name="cancellationToken">A cancellation token for async operations.</param>
	public static async Task Init(ILogReceiver logs, IEnumerable<(string name, string body)> extraPythonCommon, CancellationToken cancellationToken)
	{
		await Task.Yield();

		Directory.CreateDirectory(Paths.PythonUtilsPath);

		var pythonEngine = Python.CreateEngine();

		var paths = pythonEngine.GetSearchPaths();
		paths.Add(Paths.PythonUtilsPath);
		pythonEngine.SetSearchPaths(paths);

		var pyIO = pythonEngine.Runtime.IO;
		var outputStream = new PythonStream(logs);
		pyIO.SetOutput(outputStream, Encoding.UTF8);
		pyIO.SetErrorOutput(outputStream, Encoding.UTF8);

		foreach (var (name, body) in extraPythonCommon.Concat(GetPythonCommon()))
		{
			try
			{
				await File.WriteAllTextAsync(Path.Combine(Paths.PythonUtilsPath, name), body, cancellationToken);
			}
			catch (Exception ex)
			{
				logs.AddErrorLog(ex);
			}
		}

		ConfigManager.RegisterService(new CompilerProvider
		{
			{ FileExts.CSharp, new CSharpCompiler() },
			{ FileExts.VisualBasic, new VisualBasicCompiler() },
			{ FileExts.FSharp, new FSharpCompiler() },
			{ FileExts.Python, new PythonCompiler(pythonEngine) }
		});

		ConfigManager.RegisterService<ICustomTypeDescriptorProvider>(new PythonCustomTypeDescriptorProvider());
	}
}