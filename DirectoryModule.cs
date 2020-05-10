using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jil;

namespace FactorioRecipeCalculator
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
			var info = JSON.DeserializeDynamic(File.OpenText(infoPath));
			Name = info["name"];
			if(info.ContainsKey("version"))
				Version = new Version((string)info["version"]);
			else
				Version = new Version();
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
