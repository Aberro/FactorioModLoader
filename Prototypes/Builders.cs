#nullable enable
using System;
using System.Collections.Generic;
using System.Dynamic;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	public class IngredientBuilder
	{

		[UsedImplicitly]
		public static IIngredient Build(dynamic data)
		{
			var dic = (IDictionary<string, object>) data;
			if (!dic.ContainsKey("type"))
				return DataLoader.Current?.ProxyValue(typeof(IItemIngredient), data) ?? throw new ApplicationException();
			return dic["type"] switch
			{
				"item" => DataLoader.Current?.ProxyValue(typeof(IItemIngredient), data) ?? throw new ApplicationException(),
				"fluid" => DataLoader.Current?.ProxyValue(typeof(IFluidIngredient), data) ?? throw new ApplicationException(),
				_ => throw new ApplicationException("Invalid ingredient type!")
			};
		}
	}

	public class ProductBuilder
	{
		[UsedImplicitly]
		public static IProduct Build(dynamic data)
		{
			if (data is string)
			{
				dynamic product = new ExpandoObject();
				product.name = data;
				return DataLoader.Current?.ProxyValue(typeof(IItemProduct), data) ?? throw new ApplicationException();
			}

			var dic = (IDictionary<string, object>)data;
			if (!dic.ContainsKey("type"))
				return DataLoader.Current?.ProxyValue(typeof(IItemProduct), data) ?? throw new ApplicationException();

			return dic["type"] switch
			{
				"item" => DataLoader.Current?.ProxyValue(typeof(IItemProduct), data) ?? throw new ApplicationException(),
				"fluid" => DataLoader.Current?.ProxyValue(typeof(IFluidProduct), data) ?? throw new ApplicationException(),
				_ => throw new ApplicationException("Invalid ingredient type!")
			};
		}
	}
}
