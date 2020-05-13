using System;
using System.Collections.Generic;
using System.Dynamic;

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

	public class RecipeDataAccessor
	{
		public static IEnumerable<IProduct> Results(IRecipeData instance, dynamic data)
		{
			var dic = (IDictionary<string, object>) data;
			if (dic.ContainsKey("results") && data.results != null)
				return DataLoader.Current?.ProxyValue(typeof(IEnumerable<IProduct>), data.results) ?? throw new ApplicationException();
			else
			{
				if(!dic.ContainsKey("result"))
					throw new ApplicationException("Either 'results' or 'result' should be defined!");
				dynamic d = new ExpandoObject();
				d.name = data.result;
				d.amount = dic.ContainsKey("result_count") ? data.result_count : 1;
				var product = (IProduct)(DataLoader.Current?.ProxyValue(typeof(IProduct), d) ?? throw new ApplicationException());
				return new[] {product};
			}
		}
	}
}
