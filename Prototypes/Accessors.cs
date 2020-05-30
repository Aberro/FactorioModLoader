#nullable enable
using System;
using System.Collections.Generic;
using System.Dynamic;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	public class RecipeAccessor
	{
		private static bool _stackOverflowPrevention;

		public static IRecipeData Normal(IRecipe instance, dynamic data)
		{
			var dic = (IDictionary<string, object>) data;
			if (dic.TryGetValue("normal", out var normal))
				return (IRecipeData) (DataLoader.Current?.ProxyValue(typeof(IRecipeData), normal) ??
				                      throw new ApplicationException());
			if (dic.ContainsKey("ingredients"))
				return (IRecipeData) (DataLoader.Current?.ProxyValue(typeof(IRecipeData), data) ??
				                      throw new ApplicationException());
			try
			{
				if (_stackOverflowPrevention)
					throw new ApplicationException("RecipeData doesn't defined!");
				_stackOverflowPrevention = true;
				return instance.Expensive;
			}
			finally
			{
				_stackOverflowPrevention = false;
			}
		}

		public static IRecipeData Expensive(IRecipe instance, dynamic data)
		{
			var dic = (IDictionary<string, object>) data;
			if (dic.TryGetValue("expensive", out var expensive))
				return (IRecipeData) (DataLoader.Current?.ProxyValue(typeof(IRecipeData), expensive) ??
				                      throw new ApplicationException());
			if (dic.ContainsKey("ingredients"))
				return (IRecipeData) (DataLoader.Current?.ProxyValue(typeof(IRecipeData), data) ??
				                      throw new ApplicationException());
			try
			{
				if (_stackOverflowPrevention)
					throw new ApplicationException("RecipeData doesn't defined!");
				_stackOverflowPrevention = true;
				return instance.Normal;
			}
			finally
			{
				_stackOverflowPrevention = false;
			}
		}
	}

	public class TechnologyAccessor
	{
		private static bool _stackOverflowPrevention;
		public static ITechnologyData Normal(ITechnology instance, dynamic data)
		{
			var dic = (IDictionary<string, object>)data;
			if (dic.TryGetValue("normal", out var normal))
				return (ITechnologyData)(DataLoader.Current?.ProxyValue(typeof(ITechnologyData), normal) ??
				                     throw new ApplicationException());
			if (dic.ContainsKey("unit"))
				return (ITechnologyData)(DataLoader.Current?.ProxyValue(typeof(ITechnologyData), data) ??
				                         throw new ApplicationException());
			try
			{
				if (_stackOverflowPrevention)
					throw new ApplicationException("TechnologyData doesn't defined!");
				_stackOverflowPrevention = true;
				return instance.Expensive;
			}
			finally
			{
				_stackOverflowPrevention = false;
			}
		}
		public static ITechnologyData Expensive(ITechnology instance, dynamic data)
		{
			var dic = (IDictionary<string, object>)data;
			if (dic.TryGetValue("expensive", out var expensive))
				return (ITechnologyData)(DataLoader.Current?.ProxyValue(typeof(ITechnologyData), expensive) ??
				                         throw new ApplicationException());
			if (dic.ContainsKey("unit"))
				return (ITechnologyData)(DataLoader.Current?.ProxyValue(typeof(ITechnologyData), data) ??
				                         throw new ApplicationException());
			try
			{
				if (_stackOverflowPrevention)
					throw new ApplicationException("TechnologyData doesn't defined!");
				_stackOverflowPrevention = true;
				return instance.Normal;
			}
			finally
			{
				_stackOverflowPrevention = false;
			}
		}
	}

	public class RecipeDataAccessor
	{
		[UsedImplicitly]
#pragma warning disable IDE0060 // Remove unused parameter
		public static IEnumerable<IProduct> Results(IRecipeData instance, dynamic data)
#pragma warning restore IDE0060 // Remove unused parameter
		{
			var dic = (IDictionary<string, object>) data;
			if (dic.ContainsKey("results") && dic["results"] != null)
				return (IEnumerable<IProduct>)(DataLoader.Current?.ProxyValue(typeof(IEnumerable<IProduct>), dic["results"]) ?? throw new ApplicationException());
			else
			{
				if(!dic.ContainsKey("result"))
					throw new ApplicationException("Either 'results' or 'result' should be defined!");
				dynamic d = new ExpandoObject();
				d.name = dic["result"];
				d.amount = dic.ContainsKey("result_count") ? dic["result_count"] : 1;
				var product = (IProduct)(DataLoader.Current?.ProxyValue(typeof(IProduct), d) ?? throw new ApplicationException());
				return new[] {product};
			}
		}
	}
}
