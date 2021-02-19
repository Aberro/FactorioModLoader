#nullable enable
using System;
using JetBrains.Annotations;

namespace FactorioModLoader
{
	/// <summary>
	/// Sets possible aliases for given attribute, so dynamic proxy interceptor can try to get property value by any of the given alias names.
	/// </summary>
	[PublicAPI]
	[AttributeUsage( AttributeTargets.Property)]
	public class AliasAttribute : Attribute
	{
		public string[] Aliases { get; }

		public AliasAttribute(params string[] aliases)
		{
			Aliases = aliases;
		}
	}

	/// <summary>
	/// Used to notify dynamic proxy interceptor that instance of this class should be created by target type itself, passing data to it's constructor
	/// </summary>
	/// <remarks>
	/// Constructor should find all the necessary data in container by itself.
	/// Constructor signature should be ctor(string propertyName, dynamic container)
	/// </remarks>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Class)]
	public class InitializeByTargetType : Attribute
	{
		public string? BuilderMethod { get; }
		public InitializeByTargetType(string? builder = null)
		{
			BuilderMethod = builder;
		}
	}
	/// <summary>
	/// Used to notify dynamic proxy interceptor that instances of this class stored in repository and values of this type could be strings used as a key in such repository.
	/// </summary>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
	public class RepositoryAttribute : Attribute
	{
		public string RepositoryPath { get; }
		public RepositoryAttribute(string repositoryPath)
		{
			RepositoryPath = repositoryPath;
		}
	}

	/// <summary>
	/// Used to notify dynamic proxy interceptor that instance of this class is created by Build() method instead of constructor invocation.
	/// </summary>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class BuilderAttribute : Attribute
	{
		public Type? BuilderType { get; }
		public BuilderAttribute() { }

		public BuilderAttribute(Type builderType)
		{
			BuilderType = builderType;
		}
	}
	/// <summary>
	/// Used to notify dynamic proxy interceptor that this property is accessed through custom accessor in specified type.
	/// When accessor method has 3 parameters, value of argument is passed.
	/// </summary>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Property)]
	public class AccessorAttribute : Attribute
	{
		public Type AccessorType { get; }
		public object? Argument { get; }
		public AccessorAttribute(Type accessorType, object? argument = null)
		{
			AccessorType = accessorType;
			Argument = argument;
		}
	}
	/// <summary>
	/// Used to pass given value to property accessor when property has AccessorAttribute and it's "argument" parameter is set to null.
	/// When applied to class, given value will be passed to each property accessor when AccessorAttribute has it's "argument" parameter set to null.
	/// Accessor should have 3rd parameter of type object?.
	/// </summary>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Interface)]
	public class AccessorArgumentAttribute : Attribute
	{
		public object? Argument { get; }
		public AccessorArgumentAttribute(object? argument)
		{
			Argument = argument;
		}
	}
	/// <summary>
	/// Used to notify dynamic proxy interceptor that this property's value can be acquired by multiple ways.
	/// Each path must start from '.' dot symbol, then nested table name, or only dot symbol when instance data should be used.
	/// Example would be 'normal' and 'expensive' recipe or technology data, with data paths cosists of: "normal", ".", "expensive",
	/// so first interceptor should look for table named "normal" and use it if it's exists. If not, it'll then try to initialize
	/// property value with instance data itself, if it has all required properties (thus, without DefaultValueAttribute),
	/// and if not, it'll try to look for table named "expensive".
	/// </summary>
	public class AlternatingDataAttribute : Attribute
	{
		public string[] DataPaths { get; }
		public AlternatingDataAttribute(params string[] datapaths)
		{
			DataPaths = datapaths;
		}
	}
	/// <summary>
	/// Marks enumerable property which could have singular value with specified name
	/// </summary>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Property)]
	public class AsSingularAttribute : Attribute
	{
		public string SingularName { get; }

		public AsSingularAttribute(string singularName)
		{
			SingularName = singularName;
		}
	}
	/// <summary>
	/// Marks property as indexed in array instead of object.
	/// </summary>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Property)]
	public class IndexedAttribute : Attribute
	{
		public uint Index { get; }
		public IndexedAttribute(uint index)
		{
			Index = index;
		}
	}

}
