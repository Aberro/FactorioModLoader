using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace FactorioRecipeCalculator
{
	[DebuggerDisplay("{Name} ({Version})")]
	internal abstract class ModuleBase : IModule
	{
		protected abstract IEnumerable<string> FileNamesCache { get; }
		public string? Settings => ResolveFileName(ResolveModuleName("settings"));
		public string? SettingsUpdates => ResolveFileName(ResolveModuleName("settings-updates"));
		public string? SettingsFinalFixes => ResolveFileName(ResolveModuleName("settings-final-fixes"));
		public string? Data => ResolveFileName(ResolveModuleName("data"));
		public string? DataUpdates => ResolveFileName(ResolveModuleName("data-updates"));
		public string? DataFinalFixes => ResolveFileName(ResolveModuleName("data-final-fixes"));
		public abstract string Name { get; }
		public abstract Version Version { get; }
		public string? Description { get; protected set; }
		public abstract IEnumerable<Dependency> Dependencies { get; }
		public abstract Stream Load(string fileName);

		[return: NotNullIfNotNull("moduleName")]
		public string? ResolveModuleName(string? moduleName)
		{
			if (moduleName == null)
				return null;
			return $"__{Name}__/{moduleName.Replace('.', '/')}.lua";
		}
		public string? ResolveFileName(string? moduleName)
		{
			return moduleName != null && FileNamesCache.Contains(moduleName) ? moduleName : null;
		}

		protected IEnumerable<Dependency> LoadDependencies(dynamic info)
		{
			var list = new List<Dependency>();
			if(info.ContainsKey("dependencies"))
				foreach (var str in info["dependencies"])
				{
					if (str != null)
						list.Add(new Dependency((string)str));
				}

			if (Name == "base")
			{
				if (list.All(x => x.Name != "core"))
					list.Add(new Dependency("core"));
			}
			else if (Name != "core")
			{
				if(list.All(x => x.Name != "base"))
					list.Add(new Dependency("base"));
			}

			return list.ToArray();
		}
	}
}
