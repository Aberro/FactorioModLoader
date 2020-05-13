using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
				throw new ArgumentException("Module info not found!", nameof(path));
			dynamic info;
			using(var zipEntry = infoEntry.Open())
			{
				info = JsonSerializer.Deserialize<dynamic>(zipEntry);
			}
			Name = info["name"];
			Version = info.ContainsKey("version") ? new Version((string)info["version"]) : new Version();
			Dependencies = LoadDependencies(info);

			foreach (var entry in archive.Entries.Where(x => Path.GetExtension(x.Name) == ".lua"))
			{
				var name = entry.FullName;
				var idx = name.IndexOf(mainDirectoryName, StringComparison.InvariantCultureIgnoreCase);
				name = $"__{Name}__" + name.Substring(idx + mainDirectoryName.Length);
				var stream = new MemoryStream((int)entry.Length);
				entry.Open().CopyTo(stream);
				stream.Position = 0;
				_cache.Add(name, stream);
			}
		}

		protected override IEnumerable<string> FileNamesCache => _cache.Keys;
		public override string Name { get; }
		public override Version Version { get; }
		public override IEnumerable<Dependency> Dependencies { get; }
		public override Stream Load(string fileName)
		{
			return _cache[fileName];
		}
	}
}
