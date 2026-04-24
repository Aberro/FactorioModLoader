#nullable enable
using System;
using JetBrains.Annotations;

namespace FactorioModLoader.Prototypes
{
	[InitializeByTargetType]
	public class Energy
	{
		/// <summary>
		/// Units is in Joules
		/// </summary>
		public bool IsEnergy { get; }
		/// <summary>
		/// Units is in Watts
		/// </summary>
		public bool IsPower { get; }
		public double Units { get; }
		public static Energy Zero { get; } = new Energy(0.0);

		public Energy([NotNull] Energy copyFrom)
		{
			IsEnergy = copyFrom.IsEnergy;
			IsPower = copyFrom.IsPower;
			Units = copyFrom.Units;
		}
		public Energy(double units, bool isPower = true)
		{
			IsEnergy = !isPower;
			IsPower = isPower;
			Units = units;
		}
		public Energy(dynamic data)
		{
			if (data is string s)
			{
				if (s.EndsWith("w", StringComparison.OrdinalIgnoreCase))
				{
					IsPower = true;
					IsEnergy = false;
				}
				else if (s.EndsWith("j", StringComparison.OrdinalIgnoreCase))
				{
					IsPower = false;
					IsEnergy = true;
				}
				else
					throw new ArgumentException("Unknown energy units!");
				var multiplier = s[^2] switch
				{
					var c when (char.IsNumber(c)) => 1,
					var c when (c == 'k' || c == 'K') => Math.Pow(10, 3),
					var c when (c == 'M' || c == 'm') => Math.Pow(10, 6),
					var c when(c == 'G' || c == 'g') => Math.Pow(10, 9),
					var c when(c == 'T' || c == 't') => Math.Pow(10, 12),
					var c when(c == 'P' || c == 'p') => Math.Pow(10, 15),
					var c when(c == 'E' || c == 'e') => Math.Pow(10, 18),
					var c when(c == 'Z' || c == 'z') => Math.Pow(10, 21),
					var c when(c == 'Y' || c == 'y') => Math.Pow(10, 24),
					_ => throw new ArgumentException("Unknown energy multiplier!")
				};
				var number = s[..(char.IsNumber(s[^2])? ^1 : ^2)];
				if (!double.TryParse(number, out var value))
					throw new ArgumentException("Unknown number format!");
				Units = value * multiplier;
			}
		}
		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != this.GetType())
				return false;
			return Equals((Energy)obj);
		}
		protected bool Equals(Energy other)
		{
			return (IsEnergy == other.IsEnergy || IsPower == other.IsPower) && Units.Equals(other.Units);
		}
		public override int GetHashCode()
		{
			return HashCode.Combine(IsEnergy, IsPower, Units);
		}
		public override string ToString()
		{
			if (Units < 1000)
				return $"{Units:F}{(IsEnergy ? "J" : "W")}";
			var scaled = Units / Math.Pow(10, 3);
			if (scaled < 1000)
				return $"{scaled:F}k{(IsEnergy ? "J" : "W")}";
			scaled = Units / Math.Pow(10, 6);
			if (scaled < 1000)
				return $"{scaled:F}M{(IsEnergy ? "J" : "W")}";
			scaled = Units / Math.Pow(10, 9);
			if (scaled < 1000)
				return $"{scaled:F}G{(IsEnergy ? "J" : "W")}";
			scaled = Units / Math.Pow(10, 12);
			if (scaled < 1000)
				return $"{scaled:F}T{(IsEnergy ? "J" : "W")}";
			scaled = Units / Math.Pow(10, 15);
			if (scaled < 1000)
				return $"{scaled:F}P{(IsEnergy ? "J" : "W")}";
			scaled = Units / Math.Pow(10, 18);
			if (scaled < 1000)
				return $"{scaled:F}E{(IsEnergy ? "J" : "W")}";
			scaled = Units / Math.Pow(10, 21);
			if (scaled < 1000)
				return $"{scaled:F}Z{(IsEnergy ? "J" : "W")}";
			scaled = Units / Math.Pow(10, 24);
			return $"{scaled:F}Y{(IsEnergy ? "J" : "W")}";
		}

		public static bool operator >(Energy energy, double value) => energy.Units > value;
		public static bool operator <(Energy energy, double value) => energy.Units < value;
		public static bool operator >=(Energy energy, double value) => energy.Units >= value;
		public static bool operator <=(Energy energy, double value) => energy.Units <= value;
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		public static bool operator ==(Energy energy, double value) => energy.Units == value;
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		public static bool operator !=(Energy energy, double value) => energy.Units != value;

		public static bool operator >(double value, Energy energy) => energy.Units < value;
		public static bool operator <(double value, Energy energy) => energy.Units > value;
		public static bool operator >=(double value, Energy energy) => energy.Units <= value;
		public static bool operator <=(double value, Energy energy) => energy.Units >= value;
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		public static bool operator ==(double value, Energy energy) => energy.Units == value;
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		public static bool operator !=(double value, Energy energy) => energy.Units != value;
		/// <exception cref="InvalidOperationException">throws when one value is energy and other is power.</exception>
		public static bool operator >(Energy energy, Energy value)
		{
			if ((!energy.IsEnergy || !value.IsEnergy) && (!energy.IsPower || !value.IsPower))
				throw new InvalidOperationException("Can't compare power vs energy");
			return energy.Units > value.Units;
		}
		/// <exception cref="InvalidOperationException">throws when one value is energy and other is power.</exception>
		public static bool operator <(Energy energy, Energy value)
		{
			if ((!energy.IsEnergy || !value.IsEnergy) && (!energy.IsPower || !value.IsPower))
				throw new InvalidOperationException("Can't compare power vs energy");
			return energy.Units < value.Units;
		}
		/// <exception cref="InvalidOperationException">throws when one value is energy and other is power.</exception>
		public static bool operator >=(Energy energy, Energy value)
		{
			if ((!energy.IsEnergy || !value.IsEnergy) && (!energy.IsPower || !value.IsPower))
				throw new InvalidOperationException("Can't compare power vs energy");
			return energy.Units >= value.Units;
		}
		/// <exception cref="InvalidOperationException">throws when one value is energy and other is power.</exception>
		public static bool operator <=(Energy energy, Energy value)
		{
			if ((!energy.IsEnergy || !value.IsEnergy) && (!energy.IsPower || !value.IsPower))
				throw new InvalidOperationException("Can't compare power vs energy");
			return energy.Units <= value.Units;
		}
		/// <exception cref="InvalidOperationException">throws when one value is energy and other is power.</exception>
		public static bool operator ==(Energy energy, Energy value)
		{
			if ((!energy.IsEnergy || !value.IsEnergy) && (!energy.IsPower || !value.IsPower))
				throw new InvalidOperationException("Can't compare power vs energy");
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			return energy.Units == value.Units;
		}
		/// <exception cref="InvalidOperationException">throws when one value is energy and other is power.</exception>
		public static bool operator !=(Energy energy, Energy value)
		{
			if ((!energy.IsEnergy || !value.IsEnergy) && (!energy.IsPower || !value.IsPower))
				throw new InvalidOperationException("Can't compare power vs energy");
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			return energy.Units != value.Units;
		}
		/// <exception cref="InvalidOperationException">throws when one value is energy and other is power.</exception>
		public static Energy operator +(Energy energy, Energy value)
		{
			if ((!energy.IsEnergy || !value.IsEnergy) && (!energy.IsPower || !value.IsPower))
				throw new InvalidOperationException("Can't add power and energy");
			return new Energy(energy.Units + value.Units);
		}
		/// <exception cref="InvalidOperationException">throws when one value is energy and other is power.</exception>
		public static Energy operator -(Energy energy, Energy value)
		{
			if ((!energy.IsEnergy || !value.IsEnergy) && (!energy.IsPower || !value.IsPower))
				throw new InvalidOperationException("Can't subtract power and energy");
			return new Energy(energy.Units + value.Units);
		}
		public static double operator /(Energy energy, Energy value)
		{
			return energy.Units / value.Units;
		}
		public static Energy operator +(Energy energy, double value)
		{
			return new Energy(energy.Units + value);
		}
		public static Energy operator -(Energy energy, double value)
		{
			return new Energy(energy.Units - value);
		}
		public static Energy operator *(Energy energy, double value)
		{
			return new Energy(energy.Units * value);
		}
		public static Energy operator /(Energy energy, double value)
		{
			return new Energy(energy.Units / value);
		}
		public static Energy operator +(double value, Energy energy)
		{
			return new Energy(energy.Units + value);
		}
		public static Energy operator -(double value, Energy energy)
		{
			return new Energy(value - energy.Units);
		}
		public static Energy operator *(double value, Energy energy)
		{
			return new Energy(energy.Units * value);
		}
	}
}
