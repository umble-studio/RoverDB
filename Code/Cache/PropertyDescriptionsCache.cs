using System.Collections.Concurrent;
using System.Linq;
using RoverDB.Attributes;
using Sandbox;
using Sandbox.Internal;

namespace RoverDB.Cache;

/// <summary>
/// When we clone objects via reflection, we do so based on the available properties on the
/// class. We cache that information here, rather than deducing it on every clone.
/// </summary>
internal static class PropertyDescriptionsCache
{
	private static ConcurrentDictionary<string, PropertyDescription[]> _propertyDescriptionsCache = new();

	public static void WipeStaticFields()
	{
		_propertyDescriptionsCache = new ConcurrentDictionary<string, PropertyDescription[]>();
	}

	public static bool DoesClassHaveUniqueIdProperty( string classTypeName, object instance )
	{
		// If we have a record of it then it must do since we only cache valid types.
		if ( _propertyDescriptionsCache.TryGetValue( classTypeName, out var properties ) )
			return true;

		return GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( instance )
			.FirstOrDefault( x => x.Attributes.Any( a => a is IdAttribute ) ) is not null;
	}

	/// <summary>
	/// Returns type information for all [Saved] and [AutoSaved] properties on this class instance.
	/// </summary>
	public static PropertyDescription[] GetPropertyDescriptionsForType( string classTypeName, object instance )
	{
		if ( _propertyDescriptionsCache.TryGetValue( classTypeName, out var properties ) )
			return properties;

		properties = GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( instance )
			.Where( x => x.Attributes.Any( a => a is SavedAttribute /*or AutoSavedAttribute*/ ) )
			.ToArray();

		if ( properties.Any( x => x.Attributes.Any( a => a is IdAttribute ) ) )
			_propertyDescriptionsCache[classTypeName] = properties;

		return properties;
	}
}
