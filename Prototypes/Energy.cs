using System;
using System.Collections.Generic;
using System.Text;

namespace FactorioModLoader.Prototypes
{
	[InitializeByTargetType]
	public class Energy
	{
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
		/// <summary>
		/// Units is in Joules
		/// </summary>
		public bool IsEnergy { get; }
		/// <summary>
		/// Units is in Watts
		/// </summary>
		public bool IsPower { get; }
		public double Units { get; }
	}
}
