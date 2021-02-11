#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Utf8Json;

namespace FactorioModLoader
{
	/// <summary>
	/// Class for loading factorio modules from zip archive
	/// </summary>
	sealed class ArchiveModule : ModuleBase
	{
		private readonly Dictionary<string, MemoryStream> _cache = new Dictionary<string, MemoryStream>();

		public ArchiveModule(string path)
		{
			if (!(Directory.Exists(path) || File.Exists(path)))
				throw new ArgumentException("Module does not exists!", nameof(path));
			using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			var archive = new ZipArchive(fileStream);
			var mainDirectoryName = Path.GetDirectoryName(archive.Entries.FirstOrDefault(x =>
				!string.IsNullOrWhiteSpace(Path.GetDirectoryName(x.FullName))).FullName)?.Split('/').First();
			if (mainDirectoryName == null)
				throw new ArgumentException("Invalid mod structure!");
			var infoPath = Path.Combine(mainDirectoryName, "info.json").Replace(Path.DirectorySeparatorChar, '/');
			var infoEntry = archive.GetEntry(infoPath);
			if (infoEntry == null)
			{
				// try to find it
				infoEntry = archive.Entries.Where(x => x.Name == "info.json").FirstOrDefault();
				if(infoEntry == null)
					throw new ArgumentException("Module info not found!", nameof(path));
				mainDirectoryName = Path.GetDirectoryName(infoEntry.FullName) ?? "";
			}
			dynamic info;
			using(var zipEntry = infoEntry.Open())
			{
				info = JsonSerializer.Deserialize<dynamic>(zipEntry);
			}
			Name = info["name"];
			Title = info.ContainsKey("title") ? info["title"] : Name;
			Version = info.ContainsKey("version") ? new Version((string)info["version"]) : new Version();
			FactorioVersion = info.ContainsKey("factorio_version") ? new Version((string)info["factorio_version"]) : new Version();
			Description = info.ContainsKey("description") ? info["description"] : "";
			Author = info.ContainsKey("author") ? info["author"] : "";
			Homepage = info.ContainsKey("homepage") ? info["homepage"] : "";
			Dependencies = LoadDependencies(info);

			var extensions = new[] { ".lua", ".cfg", ".png", ".jpg" };
			var thumbnailPath = Path.Combine(mainDirectoryName, "thumbnail.png").Replace(Path.DirectorySeparatorChar, '/');
			var thumbnail = archive.GetEntry(thumbnailPath);
			foreach (var entry in archive.Entries.Where(x => extensions.Contains(Path.GetExtension(x.Name))))
			{
				var name = entry.FullName;
				var idx = name.IndexOf(mainDirectoryName, StringComparison.InvariantCultureIgnoreCase);
				name = $"__{Name}__" + name.Substring(idx + mainDirectoryName.Length);
				var stream = new MemoryStream((int)entry.Length);
				entry.Open().CopyTo(stream);
				stream.Position = 0;
				_cache.Add(name, stream);
				if (thumbnail != null && entry.FullName == thumbnail.FullName)
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
					yield return (locale, pair.Value);
				}
		}

		protected override IEnumerable<string> FileNamesCache => _cache.Keys;
		public override string Name { get; }
		public override Version Version { get; }
		public override IEnumerable<Dependency> Dependencies { get; }
		public override Stream? Load(string fileName)
		{
			if (_cache.TryGetValue(fileName, out var result))
			{
				var result_copy = new MemoryStream(result.ToArray());
				return result_copy;
			}
			return null;
		}
		public override Task<Stream?> LoadAsync(string fileName)
		{
			if (_cache.TryGetValue(fileName, out var result))
				return Task.FromResult((Stream?)result);
			return Task.FromResult((Stream?)null);
		}
	}
}
