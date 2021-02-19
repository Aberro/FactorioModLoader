#nullable enable
using System;
using System.Collections.Generic;
using System.Dynamic;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
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
