using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace FactorioModLoader
{
	public class Dependency
	{
		public enum DependencyTypeEnum
		{
			Required,
			Optional,
			HiddenOptional,
			Incompatible
		}

		public enum EqualityEnum
		{
			LessThan,
			LessOrEq,
			Equal,
			GreaterOrEq,
			GreaterThan,
		}
		private static readonly Regex Regex = new Regex(@"^\s*((?<type>[!\?]|(\(\?\)))\s*)?(?<name>(?!(PRN|AUX|CLOCK\$|NUL|CON|COM\d|LPT\d|\..*)(\..+)?)[^\x00-\x1f\\?*:\"";|\/]+?)(\s*(?<equality>[>=<]|(>=)|(<=)))?\s*(?<version>\d{1,5}(\.\d{1,5}){1,3})?\s*$");

		public IModule? Module { get; private set; }
		public Version Version { get; }
		public DependencyTypeEnum Type { get; }
		public EqualityEnum Equality { get; }
		public string Name { get; }
		public Dependency(string str)
		{
			var match = Regex.Match(str);
			if(!match.Success)
				throw new ArgumentException("Invalid dependency string!");
			Type = match.Groups["type"].Value switch
			{
				"!" => DependencyTypeEnum.Incompatible,
				"?" => DependencyTypeEnum.Optional,
				"(?)" => DependencyTypeEnum.HiddenOptional,
				_ => DependencyTypeEnum.Required
			};
			Name = match.Groups["name"].Value;
			Equality = match.Groups["equality"].Value switch
			{
				"<" => EqualityEnum.LessThan,
				"<=" => EqualityEnum.LessOrEq,
				"=" => EqualityEnum.Equal,
				">=" => EqualityEnum.GreaterOrEq,
				">" => EqualityEnum.GreaterThan,
				_ => EqualityEnum.GreaterOrEq
			};
			Version = match.Groups["version"].Success ? new Version(match.Groups["version"].Value) : new Version();
		}

		public bool DependsOn(IModule module)
		{
			if (!module.Name.Equals(Name, StringComparison.InvariantCulture))
				return false;
			if (Type != DependencyTypeEnum.Incompatible)
				return true;
			return false;
		}

		public bool ActuallyDependsOn(IModule module)
		{
			if (Module == null)
				return DependsOn(module);
			if (module == Module)
				return true;
			return Module.Dependencies.Any(subDep => subDep.ActuallyDependsOn(module));
		}

		public bool IncompatibleWith(IModule module)
		{
			if (!module.Name.Equals(Name, StringComparison.InvariantCulture))
				return false;
			if(Type == DependencyTypeEnum.Incompatible)
				return Equality switch
				{
					EqualityEnum.LessThan => module.Version < Version,
					EqualityEnum.LessOrEq => module.Version <= Version,
					EqualityEnum.Equal => module.Version == Version,
					EqualityEnum.GreaterOrEq => module.Version >= Version,
					EqualityEnum.GreaterThan => module.Version > Version,
					_ => throw new ArgumentOutOfRangeException()
				};
			return Equality switch
			{
				EqualityEnum.LessThan => module.Version >= Version,
				EqualityEnum.LessOrEq => module.Version > Version,
				EqualityEnum.Equal => module.Version != Version,
				EqualityEnum.GreaterOrEq => module.Version < Version,
				EqualityEnum.GreaterThan => module.Version <= Version,
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public void Resolve(IModule dependee)
		{
			if (dependee.Name == Name)
			{
				var versionRequirement = Equality switch
				{
					EqualityEnum.LessThan => dependee.Version < Version,
					EqualityEnum.LessOrEq => dependee.Version <= Version,
					EqualityEnum.Equal => dependee.Version == Version,
					EqualityEnum.GreaterOrEq => dependee.Version >= Version,
					EqualityEnum.GreaterThan => dependee.Version > Version,
					_ => dependee.Version >= Version
				};
				if (versionRequirement)
				{
					if (Module == null)
					{
						Module = dependee;
						return;
					}
					else
						throw new ApplicationException("Module already resolved!");
				}
			}
			throw new ArgumentException("Invalid module!", nameof(dependee));
		}
	}
}