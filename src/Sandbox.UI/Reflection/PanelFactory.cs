using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Sandbox.UI.Reflection
{
	/// <summary>
	/// Factory for creating Panel instances by name using Library and Alias attributes.
	/// This is similar to s&box's TypeLibrary.Create&lt;Panel&gt;(name) functionality.
	/// </summary>
	public static class PanelFactory
	{
		private static readonly Dictionary<string, Type> TypeRegistry = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		private static bool _initialized = false;
		private static readonly object _lock = new object();

		/// <summary>
		/// Initialize the panel type registry by scanning assemblies for Library and Alias attributes
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Types are preserved via rd.xml descriptor")]
		public static void Initialize()
		{
			if (_initialized)
				return;

			lock (_lock)
			{
				if (_initialized)
					return;

				// Scan loaded assemblies for Panel-derived types with Library or Alias attributes
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				var panelType = typeof(Panel);

				foreach (var assembly in assemblies)
				{
					try
					{
						var types = assembly.GetTypes()
							.Where(t => t.IsClass && !t.IsAbstract && panelType.IsAssignableFrom(t));

						foreach (var type in types)
						{
							// Check for Library attribute
							var libraryAttr = type.GetCustomAttribute<LibraryAttribute>();
							if (libraryAttr != null)
							{
								RegisterType(libraryAttr.Name, type);
							}

							// Check for Alias attributes
							var aliasAttrs = type.GetCustomAttributes<AliasAttribute>();
							foreach (var aliasAttr in aliasAttrs)
							{
								foreach (var alias in aliasAttr.Value)
								{
									RegisterType(alias, type);
								}
							}
						}
					}
					catch (Exception ex)
					{
						// Skip assemblies that can't be scanned
						Console.WriteLine($"Warning: Could not scan assembly {assembly.FullName}: {ex.Message}");
					}
				}

				_initialized = true;

				// Debug output
				Console.WriteLine($"PanelFactory initialized with {TypeRegistry.Count} registered types:");
				foreach (var kvp in TypeRegistry.OrderBy(x => x.Key))
				{
					Console.WriteLine($"  {kvp.Key} -> {kvp.Value.Name}");
				}
			}
		}

		private static void RegisterType(string name, Type type)
		{
			if (string.IsNullOrWhiteSpace(name))
				return;

			if (TypeRegistry.ContainsKey(name))
			{
				Console.WriteLine($"Warning: Type name '{name}' already registered as {TypeRegistry[name].Name}, skipping {type.Name}");
				return;
			}

			TypeRegistry[name] = type;
		}

		/// <summary>
		/// Create a Panel instance by its registered name (Library or Alias attribute)
		/// </summary>
		/// <param name="name">The library name or alias</param>
		/// <returns>A new Panel instance, or null if the name is not registered</returns>
		[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Types are preserved via rd.xml descriptor")]
		public static Panel? Create(string name)
		{
			if (!_initialized)
				Initialize();

			if (string.IsNullOrWhiteSpace(name))
				return null;

			if (TypeRegistry.TryGetValue(name, out var type))
			{
				try
				{
					return (Panel?)Activator.CreateInstance(type);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error creating instance of {type.Name}: {ex.Message}");
					return null;
				}
			}

			return null;
		}

		/// <summary>
		/// Check if a panel type is registered with the given name
		/// </summary>
		public static bool IsRegistered(string name)
		{
			if (!_initialized)
				Initialize();

			return !string.IsNullOrWhiteSpace(name) && TypeRegistry.ContainsKey(name);
		}

		/// <summary>
		/// Get the Type registered for the given name
		/// </summary>
		public static Type? GetType(string name)
		{
			if (!_initialized)
				Initialize();

			if (string.IsNullOrWhiteSpace(name))
				return null;

			TypeRegistry.TryGetValue(name, out var type);
			return type;
		}
	}
}
