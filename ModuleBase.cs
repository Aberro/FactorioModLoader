#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FactorioModLoader
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
		public string Title { get; protected set; } = null!;
		public abstract Version Version { get; }
		public Version? FactorioVersion { get; protected set; }
		public string? Thumbnail { get; protected set; }
		public string? Description { get; protected set; }
		public string? Author { get; protected set; }
		public string? Homepage { get; protected set; }

		public abstract IEnumerable<Dependency> Dependencies { get; }
		public abstract Stream? Load(string fileName);
		public abstract Task<Stream?> LoadAsync(string fileName);
		public async Task LoadLocalizations(IDictionary<string, IDictionary<string, IDictionary<string, string>>> localizations)
		{
			
			foreach (var localization in LoadLocalizationFiles())
			{
				using var reader = new StreamReader(localization.File);
				string? line;
				ConcurrentDictionary<string, IDictionary<string, string>> currentGroup;
				{
					if (!localizations.TryGetValue(Name, out var group))
					{
						currentGroup = new ConcurrentDictionary<string, IDictionary<string, string>>();
						if (!localizations.TryAdd(Name, currentGroup))
							currentGroup = (ConcurrentDictionary<string, IDictionary<string, string>>) localizations[""];
					}
					else
						currentGroup = (ConcurrentDictionary<string, IDictionary<string, string>>) group;
				}

				do
				{
					line = await reader.ReadLineAsync();
					if (line == null) break;
					if(string.IsNullOrWhiteSpace(line)) continue;
					var firstChar = line.Trim()[0];
					if (firstChar == '#' || firstChar == ';') continue;
					if (firstChar == '[')
					{
						var groupName = line.Trim(' ', '\t', '[', ']');
						if (!localizations.TryGetValue(groupName, out var group))
						{
							currentGroup = new ConcurrentDictionary<string, IDictionary<string, string>>();
							if (!localizations.TryAdd(groupName, currentGroup))
								currentGroup = (ConcurrentDictionary<string, IDictionary<string, string>>) localizations[groupName];
						}
						else
							currentGroup = (ConcurrentDictionary<string, IDictionary<string, string>>)group;

						continue;
					}

					var idx = line.IndexOf('=');
					if(idx < 0)
						throw new FormatException("Invalid localization file format!");
					var key = line.Substring(0, idx);
					var value = line.Substring(idx+1);
					if (!currentGroup.TryGetValue(key, out var keys))
					{
						keys = new ConcurrentDictionary<string, string>();
						if (!currentGroup.TryAdd(key, keys))
							keys = currentGroup[key];
					}

					if(!keys.TryAdd(localization.Locale, value))
						keys[localization.Locale] = value;
				} while (line != null);
				// We don't need to wait for task completion
				_ = localization.File.DisposeAsync();
			}
		}

		protected abstract IEnumerable<(string Locale, Stream File)> LoadLocalizationFiles();

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
