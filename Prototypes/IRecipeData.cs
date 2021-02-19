#nullable enable
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
		[DefaultValue(null)]
		IEnumerable<IProduct>? Results { get; }
		/// <summary>
		/// The amount of time it takes to make this recipe.
		/// This is the number of seconds it takes to craft at crafting speed 1.
		/// </summary>
		[PublicAPI]
		[DefaultValue(0.5)]
		double EnergyRequired { get; }
		[PublicAPI]
		[DefaultValue(1.0)]
		public double EmissionsMultiplier { get; }
		[PublicAPI]
		[DefaultValue(30)]
		uint RequesterPasteMultiplier { get; }
		/// <summary>
		/// Used to determine how many extra items are put into an assembling machine before it's considered "full enough"
		/// </summary>
		[PublicAPI]
		[DefaultValue(0)]
		uint OverloadMultiplier { get; }
		/// <summary>
		/// This can be false to disable the recipe at the start of the game, or "true" to leave it enabled.
		/// </summary>
		[PublicAPI]
		[DefaultValue(true)]
		bool Enabled { get; }
		/// <summary>
		/// Hides the recipe from crafting menus.
		/// </summary>
		[PublicAPI]
		[DefaultValue(false)]
		bool Hidden { get; }
		/// <summary>
		/// Hides the recipe from flow stats (item/fluid production statistics).
		/// </summary>
		[PublicAPI]
		[DefaultValue(false)]
		bool HideFromStats { get; }
		/// <summary>
		/// Hides the recipe from the player's crafting screen. The recipe will still show up for selection in machines.
		/// </summary>
		[PublicAPI]
		[DefaultValue(false)]
		bool HideFromPlayerCrafting { get; }
		/// <summary>
		/// Whether this recipe is allowed to be broken down for the recipe tooltip "Total raw" calculations.
		/// </summary>
		[PublicAPI]
		[DefaultValue(true)]
		bool AllowDecomposition { get; }
		/// <summary>
		/// Whether the recipe can be used as an intermediate recipe in hand-crafting.
		/// </summary>
		[PublicAPI]
		[DefaultValue(true)]
		bool AllowAsIntermediate { get; }
		/// <summary>
		/// Whether the recipe is allowed to use intermediate recipes when hand-crafting.
		/// </summary>
		[PublicAPI]
		[DefaultValue(true)]
		bool AllowIntermediates { get; }
		/// <summary>
		/// Whether the "Made in: {Machine}" part of the tool-tip should always be present, not only when the recipe can not be hand-crafted.
		/// </summary>
		[PublicAPI]
		[DefaultValue(false)]
		bool AlwaysShowMadeIn { get; }
		/// <summary>
		/// Whether the recipe name should have the product amount in front of it, e.g. "2 transport belt"
		/// </summary>
		[PublicAPI]
		[DefaultValue(true)]
		bool ShowAmountInTitle { get; }
		/// <summary>
		/// Whether the products are always shown in the recipe tool-tip.
		/// </summary>
		[PublicAPI]
		[DefaultValue(false)]
		bool AlwaysShowProducts { get; }
		/// <summary>
		/// Whether enabling this recipe unlocks its item products to show in selection lists (item filter, logistic request etc.).
		/// </summary>
		[PublicAPI]
		[DefaultValue(true)]
		bool UnlockResults { get; }
		/// <summary>
		/// For recipes with more than one product:
		/// This defines of which result the icon, subgroup and name is used.
		/// If it is not set and the recipe has more than 1 result the recipe will use the recipe-name and recipe-description locale and its own subgroup and icon.
		/// For recipes with 1 result:
		/// The recipe uses the icon, subgroup and name of the result by default.
		/// If set this property is set to an empty string, the recipe will use the properties of the recipe instead of the result.
		/// </summary>
		[PublicAPI]
		[DefaultValue(null)]
		IProduct? MainProduct { get; }
	}

}
