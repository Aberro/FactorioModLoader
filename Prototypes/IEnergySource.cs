using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[PublicAPI]
	public enum EnergyType
	{
		Electric,
		Burner,
		Heat,
		Fluid,
		Void,
	}

	[PublicAPI]
	public enum ElectricUsagePriority
	{
		Primary,
		Secondary,
		Tertiary,
		Solar,
		Lamp,
		None,
	}

	[PublicAPI]
	public enum FuelCategory
	{
		Chemical,
		Nuclear,
		None
	}

	[PublicAPI]
	public interface IEnergySource
	{
		[PublicAPI]
		public EnergyType Type { get; }

		[PublicAPI]
		[DefaultValue(0.0)]
		double EmissionsPerMinute { get; }
	}

	[PublicAPI]
	public interface IElectricEnergySource : IEnergySource
	{
		[PublicAPI]
		[DefaultValue(0.0)]
		Energy BufferCapacity { get; }

		[PublicAPI]
		ElectricUsagePriority UsagePriority { get; }

		[PublicAPI]
		[DefaultValue(double.PositiveInfinity)]
		Energy InputFlowLimit { get; }

		[PublicAPI]
		[DefaultValue(double.PositiveInfinity)]
		Energy OutputFlowLimit { get; }

		[PublicAPI]
		[DefaultValue(0.0)]
		Energy Drain { get; }
	}

	[PublicAPI]
	public interface IBurnerEnergySource : IEnergySource
	{
		ushort FuelInventorySize { get; }

		[PublicAPI]
		[DefaultValue(0)]
		ushort BurntInventorySize { get; }

		[PublicAPI]
		[AsSingular("fuel_category")]
		public IEnumerable<FuelCategory> FuelCategories { get; }

		[PublicAPI]
		[DefaultValue(1.0)]
		double Effectivity { get; }
	}

	[PublicAPI]
	public interface IFluidEnergySource : IEnergySource
	{
		[PublicAPI]
		IFluidBox FluidBox { get; }

		/// <summary>
		/// Optional. If set to true, the energy source will calculate power based on the fluid's fuel_value entry, else it will calculate based on fluid temperature, like Prototype/Generator.
		/// </summary>
		[PublicAPI]
		[DefaultValue(false)]
		bool BurnsFluid { get; }

		[PublicAPI]
		[DefaultValue(false)]
		bool ScaleFluidUsage { get; }

		[PublicAPI]
		[DefaultValue(0.0)]
		double FluidUsagePerTick { get; }

		[PublicAPI]
		[DefaultValue(double.PositiveInfinity)]
		double MaximumTemperature { get; }

		[PublicAPI]
		[DefaultValue(1.0)]
		double Effectivity { get; }
	}

	[PublicAPI]
	public interface IHeatEnergySource : IEnergySource
	{
		[PublicAPI]
		double MaxTemperature { get; }

		[PublicAPI]
		[DefaultValue(15.0)]
		double DefaultTemperature { get; }
		
		[PublicAPI]
		Energy SpecificHeat { get; }

		[PublicAPI]
		Energy MaxTransfer { get; }

		[PublicAPI]
		[DefaultValue(1.0)]
		double MinTemperatureGradient { get; }

		[PublicAPI]
		[DefaultValue(15.0)]
		double MinWorkingTemperature { get; }

		[PublicAPI]
		[DefaultValue(1.0)]
		double MinimumGlowTemperature { get; }
	}

	[PublicAPI]
	public interface IVoidEnergySource : IEnergySource { }

}
