using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	public interface ITechnologyData
	{
		[PublicAPI]
		[DefaultValue(false)]
		bool Upgrade { get; }

		[PublicAPI]
		[DefaultValue(null)]
		IEnumerable<ITechnology>? Prerequisites { get; }

		[PublicAPI]
		[DefaultValue(null)]
		IEnumerable<Modifier>? Effects { get; }

		[PublicAPI]
		[DefaultValue(true)]
		bool Enabled { get; }

		[PublicAPI]
		[DefaultValue(false)]
		bool Hidden { get; }

		[PublicAPI]
		[DefaultValue(false)]
		bool VisibleWhenDisabled { get; }

		[PublicAPI]
		[DefaultValue(-1)]
		int MaxLevel { get; }
		IUnit Unit { get; }
	}

	public interface IUnit
	{
		[PublicAPI]
		[DefaultValue(.0)]
		double Count { get; }

		[PublicAPI]
		[DefaultValue(null)]
		string? CountFormula { get; }

		[PublicAPI]
		double Time { get; }

		[PublicAPI]
		IEnumerable<IToolIngredient> Ingredients { get; }
	}

}
