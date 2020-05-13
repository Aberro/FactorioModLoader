using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	public interface IRecipeData
	{
		[PublicAPI]
		IEnumerable<IIngredient> Ingredients { get; }
		[PublicAPI]
		[Accessor(typeof(RecipeDataAccessor))]
		IEnumerable<IProduct> Results { get; }
		[PublicAPI]
		[DefaultValue(0.5)]
		double EnergyRequired { get; }
		[PublicAPI]
		[DefaultValue(1.0)]
		public double EmissionsMultiplier { get; }
		[PublicAPI]
		[DefaultValue(30)]
		uint RequesterPasteMultiplier { get; }
		[PublicAPI]
		[DefaultValue(0)]
		uint OverloadMultiplier { get; }
		[PublicAPI]
		[DefaultValue(true)]
		bool Enabled { get; }
		[PublicAPI]
		[DefaultValue(false)]
		bool Hidden { get; }
		[PublicAPI]
		[DefaultValue(false)]
		bool HideFromStats { get; }
	}

}
