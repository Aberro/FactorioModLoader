using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			Title = info.ContainsKey("title") ? info["title"] : Name;
			Version = info.ContainsKey("version") ? new Version((string)info["version"]) : new Version();
			FactorioVersion = info.ContainsKey("factorio_version") ? new Version((string)info["factorio_version"]) : new Version();
			Description = info.ContainsKey("description") ? info["description"] : "";
			Author = info.ContainsKey("author") ? info["author"] : "";
			Homepage = info.ContainsKey("homepage") ? info["homepage"] : "";
			Dependencies = LoadDependencies(info);

			var directoryInfo = new DirectoryInfo(path);
			var extensions = new[] {".lua", ".png", ".jpg"};
			var thumbnail = directoryInfo.GetFiles("thumbnail.*", SearchOption.TopDirectoryOnly).FirstOrDefault();
			foreach (var file in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Where(file => extensions.Contains(file.Extension)))
			{
				var name = file.FullName;
				var idx = name.IndexOf(path, StringComparison.InvariantCultureIgnoreCase);
				name = $"__{Name}__" + name.Substring(idx+path.Length).Replace('\\', '/');
				_cache.Add(name, file.FullName);
				if (file.FullName == thumbnail?.FullName)
					Thumbnail = name;
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
