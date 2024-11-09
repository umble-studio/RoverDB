using System;
using System.Collections.Concurrent;
using System.Linq;
using RoverDB.Attributes;
using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Internal;

namespace RoverDB.Extensions;

internal static class CachePropertyExtensions
{
	private static readonly ConcurrentDictionary<string, PropertyDescription[]> _propertyDescriptionsCache = new();

	public static void Wipe()
	{
		_propertyDescriptionsCache.Clear();
	}

	public static bool DoesClassHaveUniqueIdProperty( string classTypeName, object instance )
	{
		// If we have a record of it then it must do since we only cache valid types.
		if ( _propertyDescriptionsCache.TryGetValue( classTypeName, out var properties ) )
			return true;

		return GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( instance )
			.FirstOrDefault( x => x.Attributes.Any( a => a is IdAttribute ) ) is not null;
	}

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

	public static void CopyClassData<T>( T sourceClass, T destinationClass )
	{
		var properties = GetPropertyDescriptionsForType( sourceClass.GetType().FullName,
			sourceClass );

		PropertyDescription? uidProperty = null;

		foreach ( var property in properties )
		{
			var idAttr = property.GetCustomAttribute<IdAttribute>();

			if ( idAttr is not null )
			{
				uidProperty = property;
			}
			else
			{
				property.SetValue( destinationClass, property.GetValue( sourceClass ) );
			}
		}

		uidProperty?.SetValue( destinationClass, uidProperty.GetValue( sourceClass ) );
	}

	private static void CopyClassData( object sourceClass, object destinationClass )
	{
		var properties = GetPropertyDescriptionsForType( sourceClass.GetType().FullName!, sourceClass );

		PropertyDescription? uidProperty = null;

		foreach ( var property in properties )
		{
			var idAttr = property.GetCustomAttribute<IdAttribute>();

			if ( idAttr is not null )
			{
				uidProperty = property;
			}
			else
			{
				Log.Info( "Prop: " + string.Join( ", ", destinationClass.GetType(), sourceClass.GetType() ) );
				property.SetValue( destinationClass, property.GetValue( sourceClass ) );
			}
		}

		uidProperty?.SetValue( destinationClass, uidProperty.GetValue( sourceClass ) );
	}

	public static T CloneObject<T>( T theObject )
	{
		return (T)CloneObject( theObject!, typeof(T) );
	}

	public static object CloneObject( object theObject, Type objectType )
	{
		Assert.NotNull( theObject, $"{nameof(CloneObject)}: {nameof(theObject)} cannot be null" );

		var newInstance = CreateInstance( objectType );
		CopyClassData( theObject, newInstance );

		return newInstance;
	}

	private static object CreateInstance( Type objectType )
	{
		return GlobalGameNamespace.TypeLibrary.Create<object>( objectType );
	}
}
