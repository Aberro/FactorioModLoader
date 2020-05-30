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
	/// Used to notify dynamic proxy interceptor that instance of this class should be created with container object
	/// </summary>
	/// <remarks>
	/// Constructor should find all the necessary data in container by itself.
	/// Constructor signature should be ctor(string propertyName, dynamic container)
	/// </remarks>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Class)]
	public class InitializeByContainer : Attribute { }
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
	/// </summary>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Property)]
	public class AccessorAttribute : Attribute
	{
		public Type AccessorType { get; }
		public AccessorAttribute(Type accessorType)
		{
			AccessorType = accessorType;
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
