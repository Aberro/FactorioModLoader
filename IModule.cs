#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FactorioModLoader
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
		string Title { get; }
		string? Thumbnail { get; }
		string? Author { get; }
		Version Version { get; }
		Version? FactorioVersion { get; }
		string? Description { get; }
		string? Homepage { get; }
		IEnumerable<Dependency> Dependencies { get; }
		string? ResolveModuleName(string? moduleName);
		string? ResolveFileName(string? moduleName);
		Stream? Load(string fileName);
		Task<Stream?> LoadAsync(string fileName);
		Task LoadLocalizations(IDictionary<string, IDictionary<string, IDictionary<string, string>>> localizations);
	}
}
