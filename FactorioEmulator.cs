using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jil;
using MoonSharp.Interpreter;

namespace FactorioRecipeCalculator
{
	public class FactorioEmulator
	{
		private List<IModule> _activeModules = new List<IModule>();
		private readonly List<IModule> _availableModules = new List<IModule>();
		private readonly ModuleScriptLoader _loader;
		public string FactorioDirectory { get; }
		public string ModsDirectory { get; }
		public Script Lua { get; }
		public IList<IModule> ActiveModules { get; private set; }
		public IList<IModule> AvailableModules { get; }

		public event StageEventHandler? SettingsStage;
		public event StageEventHandler? SettingsUpdatesStage;
		public event StageEventHandler? SettingsFinalFixesStage;
		public event StageEventHandler? DataStage;
		public event StageEventHandler? DataUpdatesStage;
		public event StageEventHandler? DataFinalFixesStage;
		public event EventHandler? LoadingCompleted;
		public event ModuleFileEventHandler? ModuleFileLoading;

		public FactorioEmulator(string factorioPath, string mods)
		{
			if (!Directory.Exists(factorioPath))
				throw new ArgumentException("Factorio directory does not exists!");
			var data = Path.Combine(factorioPath, "Data");
			var core = Path.Combine(data, "Core");
			var @base = Path.Combine(data, "Base");
			if (!(Directory.Exists(core) && Directory.Exists(@base) && Directory.Exists(Path.Combine(core, "lualib"))))
				throw new ArgumentException("Factorio directory is invalid!");
			if (!Directory.Exists(mods))
				throw new ArgumentException("Mods directory does not exists!");
			ActiveModules = _activeModules;
			AvailableModules = _availableModules.AsReadOnly();
			FactorioDirectory = factorioPath;
			ModsDirectory = mods;
			_loader = new ModuleScriptLoader();
			Lua = new Script(CoreModules.Preset_Complete);
			Lua.Options.ScriptLoader = _loader;
			Defines.Define(Lua);
			Lua.Globals["breakpoint"] = DynValue.NewCallback(Breakpoint);
			Lua.Globals["log"] = DynValue.NewCallback(Log);
			var coreMod = new DirectoryModule(core);
			var baseMod = new DirectoryModule(@base);
			_loader.Register(coreMod);
			_loader.Register(baseMod);
			_availableModules.Add(coreMod);
			_availableModules.Add(baseMod);
			_activeModules.Add(coreMod);
			_activeModules.Add(baseMod);
			var dataLoader = "__core__/lualib/dataloader.lua";
			Lua.DoStream(_loader.Load(dataLoader), Lua.Globals, dataLoader);
			// Load zip archives
			foreach (var archive in Directory.EnumerateFiles(mods, "*.zip", SearchOption.TopDirectoryOnly))
			{
				try
				{
					_availableModules.Add(new ArchiveModule(archive));
				}
				catch
				{
					// ignored
				}
			}

			// Load unpacked mod directories
			foreach(var modFolder in Directory.EnumerateDirectories(mods))
				try
				{
					_availableModules.Add(new DirectoryModule(modFolder));
				}
				catch
				{
					// ignored
				}
		}

		public void Start()
		{
			void DefineSettings(dynamic raw, dynamic settings_startup, dynamic settings_global, dynamic settings_player, string settingType)
			{
				foreach (var setting in ((Table)raw[settingType]).Values.Select(x => x.Table))
				{
					if (setting != null)
					{
						var name = setting["name"];
						if ((string) setting["setting_type"] == "startup")
							settings_startup[name] = new Table(Lua)
							{
								["value"] = setting["default_value"]
							};
						else if ((string) setting["setting_type"] == "runtime-global")
							settings_global[name] = new Table(Lua)
							{
								["value"] = setting["default_value"]
							};
						else if ((string) setting["settings_type"] == "runtime-per-user")
							settings_player[name] = new Table(Lua)
							{
								["value"] = setting["default_value"]
							};
					}
				}
			}

			void DoStage(Func<IModule, string?> stageFileCaller)
			{
				foreach (var mod in ActiveModules)
				{
					var fileName = stageFileCaller(mod);
					if (fileName == null)
						continue;
					OnModuleFileLoading(new ModuleFileEventArgs(mod, fileName));
					ClearModules();
					Lua.DoStream(_loader.Load(fileName), Lua.Globals, fileName);
				}
			}

			void ClearModules()
			{
				((dynamic)Lua.Globals["package"])["loaded"] = new Table(Lua);
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

			ResolveDependencies();
			CheckDependencies();

			var table = new Table(Lua);
			foreach (var mod in _activeModules)
				table[mod.Name] = new Table(Lua);
			Lua.Globals["mods"] = table;

			OnSettingsStage(new StageEventArgs(Lua));
			DoStage(mod => mod.Settings);

			OnSettingsUpdatesStage(new StageEventArgs(Lua));
			DoStage(mod => mod.SettingsUpdates);

			OnSettingsFinalFixesStage(new StageEventArgs(Lua));
			DoStage(mod => mod.SettingsFinalFixes);

			//TODO: resolve settings
			var data = (dynamic) Lua.Globals["data"];
			var raw = data["raw"];
			var settings = (dynamic)(Lua.Globals["settings"] = new Table(Lua));
			var global = settings["global"] = new Table(Lua);
			var startup = settings["startup"] = new Table(Lua);
			var player = settings["player"] = new Table(Lua);
			DefineSettings(raw, startup, global, player, "bool-setting");
			DefineSettings(raw, startup, global, player, "int-setting");
			DefineSettings(raw, startup, global, player, "double-setting");
			DefineSettings(raw, startup, global, player, "string-setting");

			OnDataStage(new StageEventArgs(Lua));
			DoStage(mod => mod.Data);
			var technologies = (Table)((dynamic) Lua.Globals)["data"]["raw"]["technology"];
			foreach (var technology in technologies.Values)
			{
				if (technology == null)
					continue;
				if (technology.Table["prerequisites"] == null)
					technology.Table["prerequisites"] = new Table(Lua);
			}

			OnDataUpdatesStage(new StageEventArgs(Lua));
			DoStage(mod => mod.DataUpdates);

			OnDataFinalFixesStage(new StageEventArgs(Lua));
			DoStage(mod => mod.DataFinalFixes);

			OnLoadingCompleted();
		}
		public void LoadModList()
		{
			var modListPath = Path.Combine(ModsDirectory, "mod-list.json");
			if (!File.Exists(modListPath))
				return;
			var modList = JSON.DeserializeDynamic(File.OpenText(modListPath));
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

		public bool ActivateModule(IModule module)
		{
			if (_activeModules.Any(x => x.Name.Equals(module.Name) || x.Dependencies.Any(x => x.IncompatibleWith(module))))
				return false;
			_activeModules.Add(module);
			_loader.Register(module);
			return true;
		}

		public bool DeactivateModule(IModule module)
		{
			if (!_activeModules.Contains(module))
				return false;
			_activeModules.Remove(module);
			return true;
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
			SettingsStage?.Invoke(this, args);
		}

		protected virtual void OnSettingsUpdatesStage(StageEventArgs args)
		{
			SettingsUpdatesStage?.Invoke(this, args);
		}

		protected virtual void OnSettingsFinalFixesStage(StageEventArgs args)
		{
			SettingsFinalFixesStage?.Invoke(this, args);
		}

		protected virtual void OnDataStage(StageEventArgs args)
		{
			DataStage?.Invoke(this, args);
		}

		protected virtual void OnDataUpdatesStage(StageEventArgs args)
		{
			DataUpdatesStage?.Invoke(this, args);
		}

		protected virtual void OnDataFinalFixesStage(StageEventArgs args)
		{
			DataFinalFixesStage?.Invoke(this, args);
		}

		protected virtual void OnLoadingCompleted()
		{
			LoadingCompleted?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnModuleFileLoading(ModuleFileEventArgs args)
		{
			ModuleFileLoading?.Invoke(this, args);
		}
	}
}
