﻿#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace FactorioModLoader
{
	public class ModuleScriptLoader : IScriptLoader
	{
		private static readonly Regex ModNameRegex = new Regex(@"__(?<name>(?!(PRN|AUX|CLOCK\$|NUL|CON|COM\d|LPT\d|\..*)(\..+)?)[^\x00-\x1f\\?*:\"";|\/]+?)__");
		private readonly List<IModule> _modules = new List<IModule>();
		private IModule? _core;

		public void Register(IModule module)
		{
			if(_modules.Any(x => x == module || x.Name.Equals(module.Name, StringComparison.InvariantCultureIgnoreCase)))
				throw new ArgumentException("Module with same name already registered!");
			_modules.Add(module);
			if (module.Name == "core")
				_core = module;
		}

		public void Unregister(IModule module)
		{
			_modules.Remove(module);
		}

		public Stream? Load(string fileName)
		{
			var match = ModNameRegex.Match(fileName);
			if(!match.Success)
				throw new ArgumentException("Invalid filename format!", nameof(fileName));
			var modName = match.Groups["name"].Value;
			var mod = _modules.FirstOrDefault(x => x.Name == modName) ?? throw new ApplicationException($"Module with name {modName} not found!");
			if (mod.ResolveFileName(fileName) != null)
				return mod.Load(fileName);
			return null;
		}

		public Task<Stream?> LoadAsync(string fileName)
		{
			var match = ModNameRegex.Match(fileName);
			if (!match.Success)
				throw new ArgumentException("Invalid filename format!", nameof(fileName));
			var modName = match.Groups["name"].Value;
			var mod = _modules.FirstOrDefault(x => x.Name == modName) ?? throw new ApplicationException($"Module with name {modName} not found!");
			if (mod.ResolveFileName(fileName) != null)
				return mod.LoadAsync(fileName);
			return Task.FromResult((Stream?)null);
		}
		public object? LoadFile(string? file, Table globalContext)
		{
			if(file != null)
				return Load(file);
			return null;
		}

		public string? ResolveFileName(string? filename, Table globalContext)
		{
			return filename;
		}

		public string? ResolveModuleName(string? file, Table globalContext)
		{
			if (file == null)
				return null;
			file = file.Replace('.', '/');
			var match = ModNameRegex.Match(file);
			if (match.Success)
			{
				var moduleName = match.Groups["name"].Value;
				var module = _modules.FirstOrDefault(x => x.Name == moduleName) ?? throw new ArgumentException($"Module {moduleName} not found!", nameof(file));
				return module.ResolveFileName(file + ".lua");
			}
			else
			{
				var currentSource = globalContext.OwnerScript.CurrentSource;
				var currentModule = GetCurrentModule(globalContext.OwnerScript) ??
				                    throw new ApplicationException("Module not found!");
				var currentDirectory = Path.GetDirectoryName(currentSource)?.Replace("__"+currentModule.Name+"__", "").TrimStart(Path.DirectorySeparatorChar) ?? "";
				string? filename = Path.Combine(currentDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
				filename = currentModule.ResolveFileName(currentModule.ResolveModuleName(filename));
				if (filename != null)
					return filename;

				filename = currentModule.ResolveFileName(currentModule.ResolveModuleName(file));
				if(filename != null)
					return filename;
			}

			if (_core != null)
			{
				var coreMod = (DirectoryModule)_core;
				string? luaLibFile = coreMod.ResolveModuleName("lualib/" + file);
				luaLibFile = coreMod.ResolveFileName(luaLibFile);
				if (luaLibFile != null)
					return luaLibFile;
			}

			throw new NotImplementedException();
		}

		private IModule? GetCurrentModule(Script script)
		{
			var currentSource = script.CurrentSource;
			if (currentSource == null)
				return null;
			var match = ModNameRegex.Match(currentSource);
			if (!match.Success)
				return null;
			var currentModuleName = match.Groups["name"].Value;
			return _modules.FirstOrDefault(x => x.Name == currentModuleName);
		}
	}
}
