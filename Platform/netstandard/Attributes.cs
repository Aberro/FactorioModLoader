#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace System.Diagnostics.CodeAnalysis
{
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	internal sealed class NotNullIfNotNullAttribute : Attribute
	{
		/// <summary>Initializes the attribute with the associated parameter name.</summary>
		/// <param name="parameterName">
		/// The associated parameter name.  The output will be non-null if the argument to the parameter specified is non-null.
		/// </param>
		public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;

		/// <summary>Gets the associated parameter name.</summary>
		public string ParameterName { get; }
	}
	/// <summary>Specifies that an output may be null even if the corresponding type disallows it.</summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
	internal sealed class MaybeNullAttribute : Attribute
	{
	}
	/// <summary>Specifies that null is allowed as an input even if the corresponding type disallows it.</summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	internal sealed class AllowNullAttribute : Attribute
	{
	}
	/// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter may be null even if the corresponding type disallows it.</summary>
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class MaybeNullWhenAttribute : Attribute
	{
		/// <summary>Initializes the attribute with the specified return value condition.</summary>
		/// <param name="returnValue">
		/// The return value condition. If the method returns this value, the associated parameter may be null.
		/// </param>
		public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

		/// <summary>Gets the return value condition.</summary>
		public bool ReturnValue { get; }
	}
}
#endif