#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DocViewer
File: AssemblyUtils.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace XMLCommToHTM.DOM.Internal
{
	public static class AssemblyUtils
	{
		public static IEnumerable<Type> GetAllDerivedTypes(Assembly assembly, Type baseType)
		{
			return assembly
				.GetTypes()
				.Where(t =>
					t != baseType &&
					baseType.IsAssignableFrom(t) &&
					(t.IsPublic || t.IsNestedTypeVisible()));
		}

		private static bool IsNestedTypeVisible(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return
				type.IsNestedPublic &&
				type.IsNestedFamily &&
				type.GetBaseTypes().All(t => t.IsPublic) &&
				(type.DeclaringType == null || type.DeclaringType.IsNestedTypeVisible());
		}

		/// <summary>
		/// Загружает указанные сборки, если они еще не загружены.
		/// И возвращает для них загруженные Assembly
		/// </summary>
		/// <param name="assemblyFiles">Mассив путей к файлам сборок</param>
		/// <returns>Массив загруженных Assembly</returns>
		public static Assembly[] LoadAllAssemblies(string[] assemblyFiles)
		{
			//return assemblyFiles
			//	.Select(_ => FindLoadedAssembly(Path.GetFileName(_)) ?? Assembly.LoadFrom(_))
			//	.ToArray();

			var asms = new List<Assembly>();
			foreach (var asmFile in assemblyFiles)
			{
				Assembly asm = FindLoadedAssembly(Path.GetFileName(asmFile));
				if (asm == null)
				{
					try
					{
						asm = Assembly.LoadFrom(asmFile);
					}
					catch{}
				}
				if(asm!=null)
					asms.Add(asm);
			}
			return asms.ToArray();
		}

		public static Assembly FindLoadedAssembly(string fileName)
		{
			fileName = fileName.Trim().ToLower();
			var foundFiles = AppDomain.CurrentDomain
				                        .GetAssemblies()
				                        .Select(_ => new
					                        {
						                        Assembly = _,
						                        File = Path.GetFileName(_.GetName().CodeBase).Trim().ToLower()
					                        })
				                        .Where(_ => _.File == fileName)
				                        .ToArray();
			if (foundFiles.Length > 1) //ToDo: возможно, без ошибки выдавать первый попавшийся.
				throw new Exception("multiple assemblies loaded :" + fileName);
			if (foundFiles.Length == 0)
				return null;
			return foundFiles[0].Assembly;
		}

	}
}
