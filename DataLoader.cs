#nullable enable
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;

namespace FactorioModLoader
{
	internal class DataLoader
	{
		private readonly IProxyGenerator _generator;
		private readonly Dictionary<string, object> _repositories;

		public static DataLoader? Current { get; private set; }

		public DataLoader()
		{
			_generator = new ProxyGenerator();
			_repositories = new Dictionary<string, object>();
		}
		public IDictionary<string, T> LoadRepository<T>(string repositoryPath, dynamic data) where T : class
		{
			if(data is not ExpandoObject eo)
				throw new ApplicationException("Expected expando object!");
			var result = eo.Where(x => x.Value != null).ToDictionary(x => x.Key, x => Proxy<T>(x.Value));
			_repositories.Add(repositoryPath, result);
			return result;
		}
		public IDictionary<string, T> LoadRepositories<T>(string repositoryPath, params dynamic[] data) where T : class
		{
			if (data.Length == 1 && data[0] is dynamic[])
			{
				data = data[0];
			}
			if(data.Any(x => !(x is ExpandoObject)))
				throw new ApplicationException("Expected expando object!");
			var result = data.Cast<ExpandoObject>().SelectMany(x => x).Where(x => x.Value != null)
				.ToDictionary(x => x.Key, x => Proxy<T>(x.Value));
			_repositories.Add(repositoryPath, result);
			return result;
		}

		public object? TryGetFromRepository(string repositoryPath, string key)
		{
			_repositories.TryGetValue(repositoryPath, out var repo);
			if (repo == null) throw new ArgumentException("Repository not found!", nameof(repositoryPath));
			var dic = (IDictionary) repo;
			return dic[key];
		}

		public object? ProxyValue(Type returnType, object value)
		{
			return ProxyValue(returnType, value, null);
		}
		private object? ProxyValue(Type returnType, object value, PropertyInfo? property)
		{
			try
			{
				Current = this;
				// Interface
				if (returnType.IsInterface && (returnType.GetInterface("IEnumerable") != null || returnType.GetInterface("IList") != null))
				{
					dynamic result = typeof(List<>).MakeGenericType(returnType.GenericTypeArguments)
										 .GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object?>()) ??
									 throw new ApplicationException();
					if (value is not IList<object> list)
					{
						if (property != null && property.GetCustomAttribute<AsSingularAttribute>() != null)
							result.Add(ProxyValue(returnType.GenericTypeArguments[0], value, property));
						else
							throw new ApplicationException("Array expected!");
					}
					else
					{
						foreach (var item in list)
						{
							var val = ProxyValue(returnType.GenericTypeArguments[0], item);
							result.Add((dynamic?) val);
						}
					}

					return result;
				}

				if (value is string s)
				{
					var repositories = returnType.GetCustomAttributes<RepositoryAttribute>().ToArray();
					if (repositories != null && repositories.Any())
					{
						foreach (var repository in repositories)
						{
							var obj = TryGetFromRepository(repository.RepositoryPath, s);
							if (obj != null)
								return obj;
						}
						throw new ApplicationException($"Object with key '{s}' not found in repository!");
					}
				}


				var builderAttr = returnType.GetCustomAttribute<BuilderAttribute>(false);
				if (builderAttr != null)
				{
					var builder = builderAttr.BuilderType == null ? returnType.GetMethod("Build") : builderAttr.BuilderType.GetMethod("Build");
					if (builder != null)
					{
						return builder.Invoke(null, new[] { value }) ?? throw new NullReferenceException("Build() should never return null values!");
					}
					else
						throw new ApplicationException("Builder's Build() method not found!");
				}

				if (returnType.IsInterface && !(value is string))
				{
					return _generator.CreateInterfaceProxyWithoutTarget(returnType,
						new Interceptor(this, value));
				}

				if (returnType.IsAbstract)
				{
					return _generator.CreateClassProxy(returnType, new Interceptor(this, value));
				}

				var ctor = returnType.GetConstructor(new[] { typeof(DynamicMetaObject) });
				if (ctor != null)
				{
					return ctor.Invoke(new[] { value });
				}

				// presumably some system type
				var underlyingType = Nullable.GetUnderlyingType(returnType);
				if (underlyingType != null)
					if (value == null)
						return null;
					else
					{
						returnType = underlyingType;
					}
				if (returnType.GetInterface("IConvertible") != null && value is IConvertible convertible)
					return convertible.ToType(returnType, CultureInfo.InvariantCulture);
				if (value is ExpandoObject)
					throw new ApplicationException("Can't proxy value!");
				if(!returnType.IsInstanceOfType(value))
					throw new ApplicationException("Invalid value type!");
				return value;
			}
			catch
			{
				Current = null;
				throw;
			}
		}

		private T Proxy<T>(object data) where T : class
		{
			try
			{
				Current = this;
				return _generator.CreateInterfaceProxyWithoutTarget<T>(new Interceptor(this, data));
			}
			catch
			{
				Current = null;
				throw;
			}
		}

		private class Interceptor : IInterceptor
		{
			private readonly DataLoader _loader;
			private readonly dynamic _data;
			private readonly ConcurrentDictionary<MethodInfo, object?> _cache;
			public Interceptor(DataLoader loader, object data)
			{
				_loader = loader;

				_data = data;
				_cache = new ConcurrentDictionary<MethodInfo, object?>();
			}

			public void Intercept(IInvocation invocation)
			{
				if (_cache.TryGetValue(invocation.Method, out var result))
				{
					invocation.ReturnValue = result;
					return;
				}

				var methodName = invocation.Method.Name;
				if (!methodName.StartsWith("get_"))
					throw new ApplicationException("Only property reading is supported!");
				var propertyName = methodName[4..];
				//var dic = (IDictionary<string, object>)_data;
				result = ResolveValue(propertyName, invocation.Method.ReturnType, invocation.Proxy, _data);
				while (!_cache.TryAdd(invocation.Method, result))
				{
					if (_cache.TryGetValue(invocation.Method, out var resultNew))
					{
						result = resultNew;
						break;
					}
				}
				invocation.ReturnValue = result;
			}

			private object? ResolveValue(string propertyName, Type returnType, object proxy, dynamic data)
			{
				object? result = null;
				if (ResolveValueByContainer(ref result, returnType, propertyName)) return result;
				
				var property = ResolvePropertyInfo(propertyName, proxy);
				if (property == null)
					throw new ApplicationException($"Property {propertyName} not found!");
				var baseType = property.DeclaringType ?? throw new ApplicationException();
				if (ResolveValueByAccessor(ref result, proxy, propertyName, property, baseType))
					return result;
				if (ResolveValueByAlternatives(ref result, returnType, baseType, propertyName, data, property))
					return result;
				if (ResolveValueAsIndexed(ref result, returnType, data, property))
					return result;
				if (ResolveValueAsEntry(ref result, propertyName, returnType, data, property))
					return result;
				if (ResolveDefaultValue(ref result, returnType, property))
					return result;
				

				//if (IsNullable(property.DeclaringType, property)) return null;
				throw new ApplicationException($"Mandatory property {baseType.FullName}.{propertyName} is undefined!");
			}
			private static PropertyInfo? ResolvePropertyInfo(string propertyName, object proxy)
			{
				var proxyType = proxy.GetType();
				var baseType = proxyType.BaseType;
				PropertyInfo? propertyInfo = baseType?.GetProperty(propertyName);
				if (propertyInfo == null)
					foreach (var iface in proxyType.GetInterfaces())
					{
						propertyInfo = iface.GetProperty(propertyName);
						if (propertyInfo != null)
							break;
					}
				return propertyInfo;
			}
			private bool ResolveValueByContainer(ref object? value, Type returnType, string propertyName)
			{
				var attr = returnType.GetCustomAttribute<InitializeByTargetType>();
				if (attr == null)
					return false;
				MethodBase? builder;
				if (attr.BuilderMethod == null)
				{
					builder = returnType.GetConstructor(new[]
						{typeof(string), typeof(DynamicMetaObject)});
					if (builder == null)
						return false;
				}
				else
				{
					builder = returnType.GetMethod(attr.BuilderMethod);
					if (builder == null)
						return false;
				}
				try
				{
					value = builder?.Invoke(null, new[] {propertyName, _data});
					return true;
				}
				catch
				{
					return false;
				}
			}
			private bool ResolveValueByAccessor(ref object? value, object proxy, string propertyName, PropertyInfo property, Type baseType)
			{
				var accessorAttr = property.GetCustomAttribute<AccessorAttribute>();
				if (accessorAttr != null)
				{
					var accessorMethod = accessorAttr.AccessorType.GetMethod(propertyName);
					if (accessorMethod == null)
						throw new ApplicationException($"Accessor method {propertyName}() not found!");
					var parameters = accessorMethod.GetParameters();
					if (parameters.Length == 2)
					{
						value = accessorMethod.Invoke(null, new[] { proxy, _data });
						return true;
					}
					if (parameters.Length == 3)
					{
						var argument = accessorAttr.Argument;
						if (argument == null)
						{
							var accessorArgAttr = property.GetCustomAttribute<AccessorArgumentAttribute>();
							if (accessorArgAttr != null)
								argument = accessorArgAttr.Argument;
						}
						if (argument == null)
						{
							var accessorArgClassAttr = baseType.GetCustomAttribute<AccessorArgumentAttribute>();
							if (accessorArgClassAttr != null)
								argument = accessorArgClassAttr.Argument;
						}
						value = accessorMethod.Invoke(null, new[] { proxy, _data, argument });
						return true;
					}
				}
				return false;
			}
			bool ResolveValueAsIndexed(ref object? value, Type returnType, dynamic data, PropertyInfo property)
			{
				var indexed = property.GetCustomAttribute<IndexedAttribute>();
				if (indexed != null && data is IList<object> list)
				{
					if (list.Count <= indexed.Index)
						throw new ApplicationException("Property index is out of bounds!");
					var result = _loader.ProxyValue(returnType, list[(int)indexed.Index], property);
					if (result is ExpandoObject)
						throw new ApplicationException("Cant proxy value!");
					value = result;
					return true;
				}
				return false;
			}
			bool ResolveValueAsEntry(ref object? value, string propertyName, Type returnType, dynamic data, PropertyInfo property)
			{
				if (data is IDictionary<string, object> dic)
				{
					var key = TryGetKey(property, propertyName, dic);
					if (key != null)
					{
						var val = dic[key];
						var nullable = IsNullable(property.DeclaringType ?? throw new NullReferenceException(), property);
						if ((val == null || val is string s && s.Length == 0) && nullable)
						{
							value = null;
							return true;
						}
						var result = _loader.ProxyValue(returnType, dic[key], property);
						if (result is ExpandoObject)
							throw new ApplicationException("Cant proxy value!");
						value = result;
						return true;
					}
				}
				return false;
			}
			bool ResolveDefaultValue(ref object? value, Type returnType, PropertyInfo property)
			{
				var attr = property.GetCustomAttribute<DefaultValueAttribute>();
				if (attr == null)
					return false;
				var result = attr.Value;
				var repoAttrs = returnType.GetCustomAttributes<RepositoryAttribute>();
				if (repoAttrs != null && result is string)
				{
					value = _loader.ProxyValue(returnType, result, property);
					return true;
				}
				switch (result)
				{
					case null when !IsNullable(property.DeclaringType ?? throw new NullReferenceException(), property):
						throw new ApplicationException(
							"Default value can't be null! (Leave property as nullable reference type instead)");
					case IConvertible convertible:
						result = convertible.ToType(returnType, CultureInfo.InvariantCulture);
						break;
				}
				value = result;
				return true;
			}
			bool ResolveValueByAlternatives(ref object? value, Type returnType, Type baseType, string propertyName, dynamic data, PropertyInfo property)
			{
				var attr = property.GetCustomAttribute<AlternatingDataAttribute>();
				if (attr == null)
					return false;
				foreach (var path in attr.DataPaths)
				{
					var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
					var localData = data;
					foreach (var part in parts)
					{
						var dic = (IDictionary<string, object>)localData;
						if (!dic.ContainsKey(part))
							goto continue_main;
						localData = dic[part];
					}
					// Try it as indexed value if needed...
					var indexed = property.GetCustomAttribute<IndexedAttribute>();
					if (indexed != null && localData is IList<object> list)
					{
						if (list.Count <= indexed.Index)
							throw new ApplicationException("Property index is out of bounds!");
						var result = _loader.ProxyValue(returnType, list[(int)indexed.Index], property);
						if (result is ExpandoObject)
							throw new ApplicationException("Cant proxy value!");
						value = result;
						return true;
					}
					// Then try it as usual value
					try
					{
						bool nullable = IsNullable(property.DeclaringType ?? throw new NullReferenceException(), property);
						if (localData == null && nullable)
						{
							value = null;
							return true;
						}
						var result = _loader.ProxyValue(returnType, localData, property);
						if (result is ExpandoObject)
							throw new ApplicationException("Cant proxy value!");
						value = result;
						return true;
					}
					catch
					{
						// ignored
					}
					continue_main:
					;

				}
				if (ResolveDefaultValue(ref value, returnType, property))
					return true;
				throw new ApplicationException($"Mandatory property {baseType.FullName}.{propertyName} is undefined!");
			}
			private static string? TryGetKey(PropertyInfo property, string propertyName, IDictionary<string, object> dic)
			{
				List<string> aliases = new List<string>(3)
				{
					propertyName, FormatPropertyName(propertyName, '-'), FormatPropertyName(propertyName, '_')
				};
				var aliasAttr = property.GetCustomAttribute<AliasAttribute>(true);
				var singularAttr = property.GetCustomAttribute<AsSingularAttribute>(true);
				if (aliasAttr != null)
					aliases.AddRange(aliasAttr.Aliases);
				if(singularAttr != null)
					aliases.Add(singularAttr.SingularName);
				string? key = null;
				foreach (var name in aliases)
				{
					key = dic.Keys.FirstOrDefault(x => x.Equals(name, StringComparison.InvariantCultureIgnoreCase));
					if (key != null)
						break;
				}

				return key;
			}

			private static string FormatPropertyName(string propertyName, char ch)
			{
				var chars = new char[propertyName.Length + propertyName.Count(char.IsUpper) - 1];
				for (int i = 0, j = 0; i < propertyName.Length; i++, j++)
				{
					var c = propertyName[i];
					if (char.IsUpper(c) && i != 0)
						chars[j++] = ch;
					chars[j] = c;
				}
				return new string(chars);
			}
		}

		private static bool IsNullable(Type enclosingType, PropertyInfo? property)
		{
			if (!enclosingType.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Contains(property))
				throw new ArgumentException("enclosingType must be the type which defines property");

			var nullable = property?.CustomAttributes
				.FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
			if (nullable != null && nullable.ConstructorArguments.Count == 1)
			{
				var attributeArgument = nullable.ConstructorArguments[0];
				if (attributeArgument.ArgumentType == typeof(byte[]))
				{
					var args = (ReadOnlyCollection<CustomAttributeTypedArgument>?)attributeArgument.Value;
					if (args != null && args.Count > 0 && args[0].ArgumentType == typeof(byte))
					{
						var val = args[0].Value;
						return val != null ? (byte)val == 2 : throw new NullReferenceException();
					}
				}
				else if (attributeArgument.ArgumentType == typeof(byte))
				{
					var val = attributeArgument.Value;
					return val != null ? (byte)val == 2 : throw new NullReferenceException();
				}
			}

			var context = enclosingType.CustomAttributes
				.FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
			if (context != null &&
			    context.ConstructorArguments.Count == 1 &&
			    context.ConstructorArguments[0].ArgumentType == typeof(byte))
			{
				var val = context.ConstructorArguments[0].Value;
				return val != null ? (byte)val == 2 : throw new NullReferenceException();
			}

			// Couldn't find a suitable attribute
			return false;
		}

	}
}
