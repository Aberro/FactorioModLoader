#nullable enable
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
			FromCache = true;
			LoadRepositories();
		}

		private void LoadRepositories()
		{
			var data = (IDictionary<string, object>) Data;
			var raw = (IDictionary<string, object>)data["raw"];
			Technology = _loader.LoadRepository<ITechnology>("data.raw.technology", raw["technology"]);
			Recipe = _loader.LoadRepository<IRecipe>("data.raw.recipe", raw["recipe"]);
			Fluids = _loader.LoadRepository<IFluid>("data.raw.fluid", raw["fluid"]);
			Items = _loader.LoadRepository<IItem>("data.raw.item", raw["item"]);
		}
		public Task Save(string path)
		{
			var dir = Path.GetDirectoryName(path) ?? "";
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			using var fileStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
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
				_ => throw new ArgumentOutOfRangeException()
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
