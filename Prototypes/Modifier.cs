using System;
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
			return data.type switch
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

	public class GunSpeedModifier : ValueModifier
	{
		public string AmmoCategory { get; }
		public GunSpeedModifier(object data) : base(data)
		{
			dynamic dyn = data;
			AmmoCategory = dyn.ammo_category;
		}
	}
	public class GiveItemModifier : Modifier
	{
		public string Item { get; }
		public uint Count { get; }
		public GiveItemModifier(object data) : base(data)
		{
			dynamic dyn = data;
			Item = dyn.item;
			Count = dyn.count;
		}
	}

	public class TurretAttackModifier : ValueModifier
	{
		public string TurretId { get; }
		public TurretAttackModifier(object data) : base(data)
		{
			dynamic dyn = data;
			TurretId = dyn.turret_id;
		}
	}

	public class UnlockRecipeModifier : Modifier
	{
		public IRecipe Recipe { get; }
		public UnlockRecipeModifier(object data) : base(data)
		{
			dynamic dyn = data;
			if (DataLoader.Current == null)
				throw new ApplicationException("Unexpected behavior!");
			Recipe = DataLoader.Current.TryGetFromRepository("data.raw.recipe", dyn.recipe);
		}
	}
	public class NothingModifier : Modifier
	{
		public string EffectDescription { get; }
		public NothingModifier(object data) : base(data)
		{
			dynamic dyn = data;
			EffectDescription = dyn.effect_description;
		}
	}
	public class ValueModifier : Modifier
	{
		public object Modifier { get; }
		public ValueModifier(object data) : base(data)
		{
			dynamic dyn = data;
			Modifier = dyn.modifier;
		}
	}

}
