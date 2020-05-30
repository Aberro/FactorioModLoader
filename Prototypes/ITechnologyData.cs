using System.Collections.Generic;
using System.ComponentModel;

namespace FactorioModLoader.Prototypes
{
	public interface ITechnologyData
	{
		[DefaultValue(false)]
		bool Upgrade { get; }
		IEnumerable<ITechnology>? Prerequisites { get; }
		IEnumerable<Modifier>? Effects { get; }
		[DefaultValue(true)]
		bool Enabled { get; }
		[DefaultValue(false)]
		bool Hidden { get; }
		[DefaultValue(false)]
		bool VisibleWhenDisabled { get; }
		[DefaultValue(-1)]
		int MaxLevel { get; }
		IUnit Unit { get; }
	}

	public interface IUnit
	{
		[DefaultValue(0)]
		double Count { get; }
		[DefaultValue(null)]
		string CountFormula { get; }
		double Time { get; }
		IEnumerable<IToolIngredient> Ingredients { get; }
	}

}
