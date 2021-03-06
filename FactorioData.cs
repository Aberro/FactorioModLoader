﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FactorioModLoader.Prototypes;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Utf8Json;

namespace FactorioModLoader
{
	public class FactorioData
	{
		private readonly DataLoader _loader = new DataLoader();
		[PublicAPI]
		public bool FromCache { get; private set; }
		[PublicAPI]
		public dynamic Data { get; }
		[PublicAPI]
		public IDictionary<string, ITechnology> Technology { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IRecipe> Recipe { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IFluid> Fluids { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IItem> Items { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, ITool> Tools { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IArmor> Armor { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IRepairTool> RepairTools { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IMiningTool> MiningTools { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, Prototypes.IModule> Modules { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, ICapsule> Capsules { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IAmmoItem> Ammo { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IGun> Guns { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IItem> Rail { get; private set; } = null!;
		public IDictionary<string, IItem> SelectionTools { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IItem> ItemWithEntityData { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IItem> SpidertronRemote { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IRecipeCategory> RecipeCategory { get; private set; } = null!;

		[PublicAPI]
		public IDictionary<string, IItemSubgroup> ItemSubgroup { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IItemGroup> ItemGroup { get; private set; } = null!;
		[PublicAPI]
		public IDictionary<string, IEntity> Entities { get; set; } = null!;
		[PublicAPI]
		public IDictionary<string, IEquipment> Equipment { get; set; } = null!;
		[PublicAPI]
		public IDictionary<string, ICraftingMachine> CraftingMachines { get; set; } = null!;
		// This is internal for caching purpose
		internal IDictionary<string, IDictionary<string, IDictionary<string, string>>>? Localizations { get; set; }

		internal FactorioData(Table data)
		{
			Data = LoadFromLua(data);
			LoadRepositories();
		}

		internal static async Task<FactorioData> LoadFromCache(string cacheFile)
		{
			return await Task.Run(async () =>
			{
				await using var fileStream = File.Open(cacheFile, FileMode.Open, FileAccess.Read, FileShare.Read);
				var data = await JsonSerializer.DeserializeAsync<dynamic>(fileStream);
				return new FactorioData(data);
			});
		}
		private FactorioData(dynamic data)
		{
			Data = LoadTable(data);
			if (((IDictionary<string, object>) data).TryGetValue("__localizations", out var loc))
			{
				Localizations = new Dictionary<string, IDictionary<string, IDictionary<string, string>>>();
				foreach (var pair in (IDictionary<string, object>) loc)
				{
					var group = new Dictionary<string, IDictionary<string, string>>();
					Localizations.Add(pair.Key, group);
					foreach (var pair1 in (IDictionary<string, object>)pair.Value)
					{
						var keys = new Dictionary<string, string>();
						group.Add(pair1.Key, keys);
						foreach(var pair2 in (IDictionary<string, object>)pair1.Value)
							keys.Add(pair2.Key, (string)pair2.Value);
					}
				}
			}
			FromCache = true;
			LoadRepositories();
		}

		private void LoadRepositories()
		{
			var data = (IDictionary<string, object>) Data;
			var raw = (IDictionary<string, object>)data["raw"];
			Fluids = _loader.LoadRepository<IFluid>("data.raw.fluid", raw["fluid"]);
			Items = _loader.LoadRepository<IItem>("data.raw.item", raw["item"]);
			Tools = _loader.LoadRepository<ITool>("data.raw.tool", raw["tool"]);
			Armor = _loader.LoadRepository<IArmor>("data.raw.armor", raw["armor"]);
			Guns = _loader.LoadRepository<IGun>("data.raw.gun", raw["gun"]);
			Ammo = _loader.LoadRepository<IAmmoItem>("data.raw.ammo", raw["ammo"]);
			Capsules = _loader.LoadRepository<ICapsule>("data.raw.capsule", raw["capsule"]);
			Modules = _loader.LoadRepository<Prototypes.IModule>("data.raw.module", raw["module"]);
			MiningTools = _loader.LoadRepository<IMiningTool>("data.raw.mining-tool", raw["mining-tool"]);
			RepairTools = _loader.LoadRepository<IRepairTool>("data.raw.repair-tool", raw["repair-tool"]);
			Rail = _loader.LoadRepository<IItem>("data.raw.rail-planner", raw["rail-planner"]);
			ItemWithEntityData = _loader.LoadRepository<IItem>("data.raw.item-with-entity-data", raw["item-with-entity-data"]);
			SelectionTools = _loader.LoadRepository<IItem>("data.raw.selection-tool", raw["selection-tool"]);
			Technology = _loader.LoadRepository<ITechnology>("data.raw.technology", raw["technology"]);
			Recipe = _loader.LoadRepository<IRecipe>("data.raw.recipe", raw["recipe"]);
			SpidertronRemote = _loader.LoadRepository<IItem>("data.raw.spidertron-remote", raw["spidertron-remote"]);
			RecipeCategory = _loader.LoadRepository<IRecipeCategory>("data.raw.recipe-category", raw["recipe-category"]);
			ItemSubgroup = _loader.LoadRepository<IItemSubgroup>("data.raw.item-subgroup", raw["item-subgroup"]);
			ItemGroup = _loader.LoadRepository<IItemGroup>("data.raw.item-group", raw["item-group"]);
			var repositories = new []
			{
				"arrow", "artillery-flare", "artillery-projectile", "beam", "character-corpse",
				"cliff", "corpse", "rail-remnants", "deconstructible-tile-proxy", "entity-ghost",
				"particle", "leaf-particle", "accumulator", "artillery-turret", "beacon", "boiler",
				"burner-generator", "character", "arithmetic-combinator", "decider-combinator",
				"constant-combinator", "container", "logistic-container", "infinity-container",
				"electric-energy-interface", "electric-pole", "unit-spawner", "fish", "combat-robot",
				"construction-robot", "logistic-robot", "gate", "generator", "heat-interface", 
				"heat-pipe", "inserter", "lab", "lamp", "land-mine", "linked-container", "market", 
				"mining-drill", "offshore-pump", "pipe", "infinity-pipe", "pipe-to-ground", 
				"player-port",  "power-switch", "programmable-speaker", "pump", "radar", "curved-rail",
				"straight-rail", "rail-chain-signal", "rail-signal", "reactor", "roboport", 
				"simple-entity", "simple-entity-with-owner", "simple-entity-with-force", "solar-panel",
				"spider-leg", "storage-tank", "train-stop", "linked-belt", "loader-1x1", "loader",
				"splitter", "transport-belt", "underground-belt", "tree", "turret", "ammo-turret",
				"electric-turret", "fluid-turret", "unit", "car", "artillery-wagon", "cargo-wagon",
				"fluid-wagon", "locomotive", "spider-vehicle", "wall", "explosion", 
				"flame-thrower-explosion", "fire", "stream", "flying-text", "highlight-box",
				"item-entity", "item-request-proxy", "particle-source", "projectile", "resource",
				"rocket-silo-rocket", "rocket-silo-rocket-shadow", "smoke", "smoke-with-trigger",
				"speech-bubble", "sticker", "tile-ghost",
			};
			Entities = _loader.LoadRepositories<IEntity>("data.raw.entity",
				raw.Where(x => repositories.Contains(x.Key)).Select(x => x.Value).ToArray());

			repositories = new[]
			{
				"active-defense-equipment", "battery-equipment", "belt-immunity-equipment",
				"energy-shield-equipment", "generator-equipment", "movement-bonus-equipment",
				"night-vision-equipment", "roboport-equipment", "solar-panel-equipment"

			};
			Equipment = _loader.LoadRepositories<IEquipment>("data.raw.equipment",
				raw.Where(x => repositories.Contains(x.Key)).Select(x => x.Value).ToArray());
			repositories = new[]
			{
				"assembling-machine", "rocket-silo", "furnace"
			};
			CraftingMachines = _loader.LoadRepositories<ICraftingMachine>("data.raw.crafting-machine",
				raw.Where(x => repositories.Contains(x.Key)).Select(x => x.Value).ToArray());
		}

		public Task Save(string path)
		{
			var dir = Path.GetDirectoryName(path) ?? "";
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			using var fileStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
			if(Localizations != null)
				((IDictionary<string, object>)Data)["__localizations"] = Localizations;
			return JsonSerializer.SerializeAsync<dynamic>(fileStream, Data);
		}

		private dynamic LoadFromLua(Table data)
		{
			FromCache = false;
			return LoadTable(data);
		}

		private dynamic LoadTable(dynamic table)
		{
			if (table is IList<object> list)
			{
				var result = new List<dynamic>(list.Count);
				foreach (var item in list)
					result.Add(LoadValue(item));
				return result.ToArray();
			}
			else if (table is IDictionary<string, object> obj)
			{
				var result = new ExpandoObject();
				foreach (var pair in obj)
				{
					var key = pair.Key;
					if(key != null)
						CollectionExtensions.TryAdd(result, key, LoadValue(pair.Value));
				}
				return result;
			}
			else
				throw new InvalidCastException("Invalid dynamic type");
		}
		private dynamic LoadValue(dynamic value)
		{
			if (value is IList<object> || value is IDictionary<string, object>)
				return LoadTable(value);
			return value;
		}

		private dynamic LoadTable(Table table)
		{
			if (table.Keys.All(x => x.Type == DataType.Number))
			{
				var result = new List<dynamic>(table.Length);
				foreach (var pair in table.Pairs)
					if(pair.Value != null && pair.Value.Type != DataType.Nil)
						result.Add(LoadValue(pair.Value));
				return result.ToArray();
			}
			else
			{
				var result = new ExpandoObject();
				foreach (var pair in table.Pairs)
				{
					if (!IsSerializable(pair.Value))
						continue;
					if (pair.Key.Type != DataType.Number && pair.Key.Type != DataType.String)
						continue;
					var key = pair.Key.String ?? pair.Key.Number.ToString(CultureInfo.InvariantCulture);
					CollectionExtensions.TryAdd(result, key, LoadValue(pair.Value));
				}

				return result;
			}
		}

		private dynamic? LoadValue(DynValue value)
		{
			if (!IsSerializable(value))
				throw new ApplicationException("Non-serializable value!");
			return value.Type switch
			{
				DataType.Nil => null,
				DataType.Void => null,
				DataType.Boolean => value.Boolean,
				DataType.Number => value.Number,
				DataType.String => value.String,
				DataType.Table => LoadTable(value.Table),
				DataType.Tuple => value.Tuple.Where(IsSerializable).Select(LoadValue).ToArray(),
				DataType.UserData => value.UserData.Object,
				_ => throw new ArgumentOutOfRangeException(nameof(value))
			};
		}

		private bool IsSerializable(DynValue value)
		{
			return (value.Type) switch
			{
				DataType.Function => false,
				DataType.Thread => false,
				DataType.ClrFunction => false,
				DataType.TailCallRequest => false,
				DataType.YieldRequest => false,
				_ => true
			};
		}
	}
}
