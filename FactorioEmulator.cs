#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Utf8Json;

namespace FactorioModLoader
{
	[PublicAPI]
	public class FactorioEmulator
	{
		private List<IModule> _activeModules;
		private readonly List<IModule> _availableModules;
		private readonly ModuleScriptLoader _loader;
		private bool _hasStarted;
		private readonly Script _lua;

		[PublicAPI]
		public string FactorioDirectory { get; }
		[PublicAPI]
		public string ModsDirectory { get; }
		[PublicAPI]
		public string CachePath { get; }
		[PublicAPI] 
		public bool IsLoading => _hasStarted && Data == null;

		[PublicAPI]
		public Script Lua
		{
			get
			{
				if(_hasStarted && Data == null)
					throw new InvalidOperationException($"Can't access {nameof(Lua)} property while running!");
				return _lua;
			}
		}

		[PublicAPI]
		public IList<IModule> ActiveModules { get; private set; }
		[PublicAPI]
		public IList<IModule> AvailableModules { get; }
		[PublicAPI]
		public FactorioData? Data { get; private set; }
		[PublicAPI]
		public bool CacheExists => File.Exists(CachePath);

		[PublicAPI]
		public bool Cached => Data != null;
		[PublicAPI]
		public event StageEventHandler? SettingsStage;
		[PublicAPI]
		public event StageEventHandler? SettingsUpdatesStage;
		[PublicAPI]
		public event StageEventHandler? SettingsFinalFixesStage;
		[PublicAPI]
		public event StageEventHandler? DataStage;
		[PublicAPI]
		public event StageEventHandler? DataUpdatesStage;
		[PublicAPI]
		public event StageEventHandler? DataFinalFixesStage;
		[PublicAPI]
		public event EventHandler? LoadingCompleted;
		[PublicAPI]
		public event EventHandler? LoadingError;
		[PublicAPI]
		public event ModuleFileEventHandler? ModuleFileLoading;
		[PublicAPI]
		public event ModuleEventHandler? ModuleActivated;
		[PublicAPI]
		public event ModuleEventHandler? ModuleDeactivated;

		[PublicAPI]
		public static async Task<FactorioEmulator> Create(string factorioPath, string modsPath, string cachePath)
		{
			if (!Directory.Exists(factorioPath))
				throw new ArgumentException("Factorio directory does not exists!");
			var data = Path.Combine(factorioPath, "Data");
			var core = Path.Combine(data, "Core");
			var @base = Path.Combine(data, "Base");
			if (!(Directory.Exists(core) && Directory.Exists(@base) && Directory.Exists(Path.Combine(core, "lualib"))))
				throw new ArgumentException("Factorio directory is invalid!");
			if (!Directory.Exists(modsPath))
				throw new ArgumentException("Mods directory does not exists!");
			var archiveLoading = Task.WhenAll(Directory.EnumerateFiles(modsPath, "*.zip", SearchOption.TopDirectoryOnly)
				.Select(
					archive =>
						Task.Run(() =>
						{
							try
							{
								return (IModule?)new ArchiveModule(archive);
							}
							catch
							{
								return null;
							}
						})));
			var directoryLoading = Task.WhenAll(Directory
				.EnumerateDirectories(modsPath, "*", SearchOption.TopDirectoryOnly).Select(
					archive =>
						Task.Run(() =>
						{
							try
							{
								return (IModule?)new DirectoryModule(archive);
							}
							catch
							{
								return null;
							}
						})));

			var result = new FactorioEmulator(cachePath, factorioPath, modsPath, (await archiveLoading).Concat(await directoryLoading));
			return result;
		}

		private FactorioEmulator(string cachePath, string factorioPath, string modsPath, IEnumerable<IModule?> availableModules)
		{
			// ReSharper disable once RedundantEnumerableCastCall
			_availableModules = availableModules.Where(module => module != null).Cast<IModule>().ToList();
			_activeModules = new List<IModule>();
			CachePath = cachePath;
			AvailableModules = _availableModules.AsReadOnly();
			ActiveModules = _activeModules;
			FactorioDirectory = factorioPath;
			ModsDirectory = modsPath;
			_loader = new ModuleScriptLoader();
			_lua = new Script(CoreModules.Preset_Complete) { Options = { ScriptLoader = _loader } };
			Defines.Define(_lua);
			_lua.Globals["breakpoint"] = DynValue.NewCallback(Breakpoint);
			_lua.Globals["log"] = DynValue.NewCallback(Log);
			var data = Path.Combine(factorioPath, "Data");
			var core = Path.Combine(data, "Core");
			var @base = Path.Combine(data, "Base");
			var coreMod = new DirectoryModule(core);
			var baseMod = new DirectoryModule(@base);
			_loader.Register(coreMod);
			_loader.Register(baseMod);
			_availableModules.Add(coreMod);
			_availableModules.Add(baseMod);
			_activeModules.Add(coreMod);
			_activeModules.Add(baseMod);
			var dataLoader = "__core__/lualib/dataloader.lua";
			_lua.DoStream(_loader.Load(dataLoader), _lua.Globals, dataLoader);
		}

		public async Task LoadCached()
		{
			_hasStarted = true;
			if(!CacheExists)
				throw new InvalidOperationException("Cache file doesn't exists!");
			Data = await FactorioData.LoadFromCache(CachePath);
			OnLoadingCompleted();
		}

		[PublicAPI]
		public async void Start()
		{
			_hasStarted = true;
			// ReSharper disable VariableHidesOuterVariable
			void DefineSettings(dynamic raw, dynamic settingsStartup, dynamic settingsGlobal, dynamic settingsPlayer, string settingType)
				// ReSharper restore VariableHidesOuterVariable
			{
				var settings = raw[settingType];
				if(settings != null)
					foreach (var setting in ((Table)raw[settingType]).Values.Select(x => x.Table))
					{
						if (setting != null)
						{
							var name = setting["name"];
							if ((string) setting["setting_type"] == "startup")
								settingsStartup[name] = new Table(_lua)
								{
									["value"] = setting["default_value"]
								};
							else if ((string) setting["setting_type"] == "runtime-global")
								settingsGlobal[name] = new Table(_lua)
								{
									["value"] = setting["default_value"]
								};
							else if ((string) setting["settings_type"] == "runtime-per-user")
								settingsPlayer[name] = new Table(_lua)
								{
									["value"] = setting["default_value"]
								};
						}
					}
			}

			Task DoStage(Func<IModule, string?> stageFileCaller)
			{
				return Task.Factory.StartNew(() =>
				{
					foreach (var mod in ActiveModules)
					{
						var fileName = stageFileCaller(mod);
						if (fileName == null)
							continue;
						OnModuleFileLoading(new ModuleFileEventArgs(mod, fileName));
						ClearModules();
						_lua.DoStream(_loader.Load(fileName), _lua.Globals, fileName);
					}
				});
			}

			void ClearModules()
			{
				((dynamic)_lua.Globals["package"])["loaded"] = new Table(_lua);
			}

			void ResolveDependencies()
			{
				foreach(var mod in ActiveModules)
					foreach(var dep in mod.Dependencies)
					{
						var dependee = ActiveModules.FirstOrDefault(x => x.Name == dep.Name);
						if(dependee != null)
							dep.Resolve(dependee);
					}

				var sorted = new List<IModule>(ActiveModules.Count);
				foreach (var x in ActiveModules)
				{
					var bestMatch = 0;
					int idx = -1;
					foreach (var y in sorted)
					{
						idx++;
						var xDepY = x.Dependencies.Any(mod => mod.ActuallyDependsOn(y));
						var yDepX = y.Dependencies.Any(mod => mod.ActuallyDependsOn(x));
						if (xDepY && yDepX)
							throw new ApplicationException($"Circular dependency found: {x} and {y}");
						if (xDepY)
						{
							bestMatch = idx+1;
							continue;
						}

						if (yDepX)
						{
							break;
						}

						if (String.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase) > 0)
							bestMatch = idx+1;
					}
					sorted.Insert(bestMatch, x);
				}

				_activeModules = sorted;
				ActiveModules = sorted.AsReadOnly();
			}

			void CheckDependencies()
			{
				if(ActiveModules[0].Name != "core")
					throw new ApplicationException("core should always be the first loaded module!");
				if(ActiveModules[1].Name != "base")
					throw new ApplicationException("base should always be the second loaded module!");
				for(int i = 0; i < ActiveModules.Count; i++)
				{
					var mod = ActiveModules[i];
					foreach (var dep in mod.Dependencies)
					{
						var dependee = ActiveModules.FirstOrDefault(x => x.Name == dep.Name);
						if (dependee != null)
						{
							if (ActiveModules.IndexOf(dependee) > i)
								throw new ApplicationException("Dependency order is compromised!");
						}
						else if(dep.Type == Dependency.DependencyTypeEnum.Required)
								throw new ApplicationException("Dependency doesn't met!");
					}
				}
			}

			try
			{
				ResolveDependencies();
				CheckDependencies();

				var table = new Table(_lua);
				foreach (var mod in _activeModules)
					table[mod.Name] = new Table(_lua);
				_lua.Globals["mods"] = table;

				OnSettingsStage(new StageEventArgs(_lua));
				await DoStage(mod => mod.Settings);

				OnSettingsUpdatesStage(new StageEventArgs(_lua));
				await DoStage(mod => mod.SettingsUpdates);

				OnSettingsFinalFixesStage(new StageEventArgs(_lua));
				await DoStage(mod => mod.SettingsFinalFixes);

				var data = (dynamic) _lua.Globals["data"];
				if (data["raw"] == null)
					data["raw"] = new Table(_lua);
				var raw = data["raw"];
				var settings = (dynamic) (_lua.Globals["settings"] = new Table(_lua));
				var global = settings["global"] = new Table(_lua);
				var startup = settings["startup"] = new Table(_lua);
				var player = settings["player"] = new Table(_lua);
				DefineSettings(raw, startup, global, player, "bool-setting");
				DefineSettings(raw, startup, global, player, "int-setting");
				DefineSettings(raw, startup, global, player, "double-setting");
				DefineSettings(raw, startup, global, player, "string-setting");

				OnDataStage(new StageEventArgs(_lua));
				await DoStage(mod => mod.Data);
				var technologies = (Table) ((dynamic) _lua.Globals)["data"]["raw"]["technology"];
				foreach (var technology in technologies.Values)
				{
					if (technology == null)
						continue;
					technology.Table["prerequisites"] ??= new Table(_lua);
				}

				OnDataUpdatesStage(new StageEventArgs(_lua));
				await DoStage(mod => mod.DataUpdates);

				OnDataFinalFixesStage(new StageEventArgs(_lua));
				await DoStage(mod => mod.DataFinalFixes);

				var fData = new FactorioData((Table) _lua.Globals["data"]);
				await fData.Save(CachePath);
				Data = fData;
				OnLoadingCompleted();
			}
			catch(Exception e)
			{
				OnLoadingError(e.Message);
				throw;
			}
		}
		[PublicAPI]
		public void LoadModList()
		{
			if(_hasStarted)
				throw new InvalidOperationException("Can't load mod-list.json after FactorioEmulator.Start() invocation!");
			var modListPath = Path.Combine(ModsDirectory, "mod-list.json");
			if (!File.Exists(modListPath))
				return;
			var modList = JsonSerializer.Deserialize<dynamic>(File.Open(modListPath, FileMode.Open, FileAccess.Read, FileShare.Read));
			foreach (var mod in modList["mods"])
			{
				if(mod == null)
					continue;
				var enabled = (bool)mod["enabled"];
				if (!enabled)
					continue;
				var name = (string)mod["name"];
				var version = (Version?)null;
				if (mod.ContainsKey("version"))
					version = new Version((string)mod["version"]);
				if (version != null)
				{
					var match = _availableModules.Find(x =>
						x.Name.Equals(name, StringComparison.InvariantCulture) && x.Version == version);
					if(match != null)
						ActivateModule(match);
					else
						System.Diagnostics.Debug.WriteLine($"Module \"{name}\" not found!");
				}
				else
				{
					var match = _availableModules.Where(x => x.Name == name).OrderByDescending(x => x.Version).First();
					ActivateModule(match);
				}
			}
		}

		[PublicAPI]
		public bool ActivateModule(IModule module)
		{
			if (_hasStarted)
				throw new InvalidOperationException("Can't activate module after FactorioEmulator.Start() invocation!");
			if (_activeModules.Any(x => x.Name.Equals(module.Name) || x.Dependencies.Any(dep => dep.IncompatibleWith(module))))
				return false;
			_activeModules.Add(module);
			_loader.Register(module);
			OnModuleActivated(module);
			return true;
		}
		[PublicAPI]
		public bool DeactivateModule(IModule module)
		{
			if (_hasStarted)
				throw new InvalidOperationException("Can't deactivate module after FactorioEmulator.Start() invocation!");
			if (!_activeModules.Contains(module))
				return false;
			_activeModules.Remove(module);
			_loader.Unregister(module);
			OnModuleDeactivated(module);
			return true;
		}
		public Stream? LoadFile(string path)
		{
			var modname = _loader.ResolveModuleName(path, _lua.Globals);
			var filename = _loader.ResolveFileName(modname, _lua.Globals);
			if (filename != null)
				return _loader.Load(filename);
			return null;
		}

		private DynValue Breakpoint(ScriptExecutionContext context, CallbackArguments args)
		{
			return DynValue.Nil;
		}

		private DynValue Log(ScriptExecutionContext context, CallbackArguments args)
		{
			if (args.Count > 0)
				System.Diagnostics.Debug.WriteLine(args[0]);
			return DynValue.Nil;
		}

		protected virtual void OnSettingsStage(StageEventArgs args)
		{
			try
			{
				SettingsStage?.Invoke(this, args);
			}
			catch { /**/ }
		}

		protected virtual void OnSettingsUpdatesStage(StageEventArgs args)
		{
			try
			{
				SettingsUpdatesStage?.Invoke(this, args);
			}
			catch { /**/ }
		}

		protected virtual void OnSettingsFinalFixesStage(StageEventArgs args)
		{
			try
			{
				SettingsFinalFixesStage?.Invoke(this, args);
			}
			catch { /**/ }
		}

		protected virtual void OnDataStage(StageEventArgs args)
		{
			try
			{
				DataStage?.Invoke(this, args);
			}
			catch { /**/ }
		}

		protected virtual void OnDataUpdatesStage(StageEventArgs args)
		{
			try
			{
				DataUpdatesStage?.Invoke(this, args);
			}
			catch { /**/ }
		}

		protected virtual void OnDataFinalFixesStage(StageEventArgs args)
		{
			try
			{
				DataFinalFixesStage?.Invoke(this, args);
			}
			catch { /**/ }
		}

		protected virtual void OnLoadingCompleted()
		{
			try
			{
				LoadingCompleted?.Invoke(this, EventArgs.Empty);
			}
			catch { /**/ }
		}

		protected virtual void OnModuleFileLoading(ModuleFileEventArgs args)
		{
			try
			{
				ModuleFileLoading?.Invoke(this, args);
			}
			catch { /**/ }
		}

		protected virtual void OnModuleActivated(IModule module)
		{
			try
			{
				ModuleActivated?.Invoke(this, new ModuleEventArgs(module));
			}
			catch { /**/ }
}
		protected virtual void OnModuleDeactivated(IModule module)
		{
			try
			{
				ModuleDeactivated?.Invoke(this, new ModuleEventArgs(module));
			}
			catch { /**/ }
		}

		protected virtual void OnLoadingError(string error)
		{
			try
			{
				LoadingError?.Invoke(this, new LoadingErrorEventArgs(error));
			}
			catch { /**/ }
		}
	}
}
