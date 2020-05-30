#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[Builder]
	public class Modifier
	{
		protected Modifier([UsedImplicitly]dynamic data)
		{

		}
		[PublicAPI]
		public static Modifier Build(dynamic data)
		{
			var dic = data as IDictionary<string, object> ?? throw new ApplicationException();
			return dic["type"] switch
			{
				"gun-speed" => new GunSpeedModifier(data),
				"give-item" => new GiveItemModifier(data),
				"turret-attack" => new TurretAttackModifier(data),
				"unlock-recipe" => new UnlockRecipeModifier(data),
				"nothing" => new NothingModifier(data),
				_ => new ValueModifier(data)
			};
		}
	}
	[PublicAPI]
	public class GunSpeedModifier : ValueModifier
	{
		[PublicAPI]
		public string AmmoCategory { get; }
		public GunSpeedModifier(object data) : base(data)
		{
			var dic = (IDictionary<string, object>)data;
			AmmoCategory = (string)dic["ammo_category"];
		}
	}
	[PublicAPI]
	public class GiveItemModifier : Modifier
	{
		[PublicAPI]
		public string Item { get; }
		[PublicAPI]
		public uint Count { get; }
		public GiveItemModifier(object data) : base(data)
		{
			var dic = (IDictionary<string, object>)data;
			Item = (string)dic["item"];
			Count = (uint)dic["count"];
		}
	}

	[PublicAPI]
	public class TurretAttackModifier : ValueModifier
	{
		[PublicAPI]
		public string TurretId { get; }
		public TurretAttackModifier(object data) : base(data)
		{
			var dic = (IDictionary<string, object>)data;
			TurretId = (string)dic["turret_id"];
		}
	}

	[PublicAPI]
	public class UnlockRecipeModifier : Modifier
	{
		[PublicAPI]
		public IRecipe Recipe { get; }
		public UnlockRecipeModifier(object data) : base(data)
		{
			var dic = data as IDictionary<string, object> ?? throw new ApplicationException();
			if (DataLoader.Current == null)
				throw new ApplicationException("Unexpected behavior!");
			Recipe = (IRecipe)(DataLoader.Current.TryGetFromRepository("data.raw.recipe",
				         (string) (dic["recipe"] ?? throw new ApplicationException())) ??
			         throw new ApplicationException($"Recipe with key '{dic["recipe"]}' not found in repository!"));
		}
	}
	[PublicAPI]
	public class NothingModifier : Modifier
	{
		[PublicAPI]
		public string EffectDescription { get; }
		public NothingModifier(object data) : base(data)
		{
			var dic = (IDictionary<string, object>)data;
			EffectDescription = (string)dic["effect_description"];
		}
	}
	[PublicAPI]
	public class ValueModifier : Modifier
	{
		[PublicAPI]
		public object Modifier { get; }
		public ValueModifier(object data) : base(data)
		{
			var dic = (IDictionary<string, object>)data;
			Modifier = dic["modifier"];
		}
	}

}
