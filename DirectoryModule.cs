using System;
using System.Collections.Generic;
using System.IO;
using Utf8Json;

namespace FactorioModLoader
{
	/// <summary>
	/// Class for loading factorio modules from directory
	/// </summary>
	sealed class DirectoryModule : ModuleBase
	{
		private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
		public DirectoryModule(string path)
		{
			if(!Directory.Exists(path))
				throw new ArgumentException("Module does not exists!", nameof(path));
			var infoPath = Path.Combine(path, "info.json");
			if (!File.Exists(infoPath))
				throw new ArgumentException("Module info not found!", nameof(path));
			var info = JsonSerializer.Deserialize<dynamic>(File.Open(infoPath, FileMode.Open, FileAccess.Read, FileShare.Read));
			Name = info["name"];
			Version = info.ContainsKey("version") ? new Version((string)info["version"]) : new Version();
			Dependencies = LoadDependencies(info);

			foreach (var file in Directory.EnumerateFiles(path, "*.lua", SearchOption.AllDirectories))
			{
				var name = file;
				var idx = file.IndexOf(path, StringComparison.InvariantCultureIgnoreCase);
				name = $"__{Name}__" + name.Substring(idx+path.Length).Replace('\\', '/');
				_cache.Add(name, file);
			}
		}

		protected override IEnumerable<string> FileNamesCache => _cache.Keys;
		public override string Name { get; }
		public override Version Version { get; }
		public override IEnumerable<Dependency> Dependencies { get; }
		public override Stream Load(string fileName)
		{
			return File.Open(_cache[fileName], FileMode.Open, FileAccess.Read, FileShare.Read);
		}
	}
}
