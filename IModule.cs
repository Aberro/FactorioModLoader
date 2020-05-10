using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FactorioRecipeCalculator
{
	public interface IModule
	{
		string? Settings { get; }
		string? SettingsUpdates { get; }
		string? SettingsFinalFixes { get; }
		string? Data { get; }
		string? DataUpdates { get; }
		string? DataFinalFixes { get; }
		string Name { get; }
		Version Version { get; }
		string? Description { get; }
		IEnumerable<Dependency> Dependencies { get; }
		string? ResolveModuleName(string? moduleName);
		string? ResolveFileName(string? moduleName);
		Stream Load(string fileName);
	}
}
