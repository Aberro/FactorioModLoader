#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
			var extensions = new[] {".lua", ".cfg", ".png", ".jpg"};
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

		protected override IEnumerable<(string Locale, Stream File)> LoadLocalizationFiles()
		{
			var localePath = $"__{Name}__/locale/";
			foreach (var pair in _cache)
				if (pair.Key.StartsWith(localePath))
				{
					var locale = pair.Key.Substring(localePath.Length).Split('/')[0];
					if (string.IsNullOrWhiteSpace(locale))
						throw new ApplicationException();
					yield return (locale, File.OpenRead(pair.Value));
				}
		}

		protected override IEnumerable<string> FileNamesCache => _cache.Keys;
		public override string Name { get; }
		public override Version Version { get; }
		public override IEnumerable<Dependency> Dependencies { get; }
		public override Stream? Load(string fileName)
		{
			if (_cache.TryGetValue(fileName, out var result))
				return File.Open(result, FileMode.Open, FileAccess.Read, FileShare.Read);
			return null;
		}
		public override Task<Stream?> LoadAsync(string fileName)
		{
			if (_cache.TryGetValue(fileName, out var result))
				return Task.Run(() => (Stream?)File.Open(result, FileMode.Open, FileAccess.Read, FileShare.Read));
			return Task.FromResult((Stream?)null);
		}
	}
}
