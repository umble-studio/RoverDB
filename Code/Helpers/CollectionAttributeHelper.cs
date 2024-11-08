using System;
using RoverDB.Attributes;
using Sandbox;

namespace RoverDB.Helpers;

public static class CollectionAttributeHelper
{
	public static bool IsBaseType( Type type )
	{
		var t = TypeLibrary.GetType( type );

		if ( !TryGetAttribute( t, out var isBaseType, out _ ) )
			return false;

		return isBaseType;
	}

	public static Type? GetCollectionType( this Type type )
	{
		var t = TypeLibrary.GetType( type );
		var collectionAttr = t.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
			return t.TargetType;

		collectionAttr = t.BaseType.GetAttribute<CollectionAttribute>();
		return collectionAttr is not null ? t.BaseType.TargetType : null;
	}

	public static bool TryGetAttribute( TypeDescription type, out bool isBaseType, out CollectionAttribute? attribute )
	{
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is null )
		{
			Log.Info( "BaseType: " + type.BaseType.Name );

			// Check if its ancestor is have the collection attribute
			collectionAttr = type.BaseType.GetAttribute<CollectionAttribute>();

			if ( collectionAttr is not null )
			{
				isBaseType = true;
				attribute = collectionAttr;
				return true;
			}

			Log.Error( $"Type {type.FullName} is not a collection" );

			isBaseType = false;
			attribute = null;
			return false;
		}

		isBaseType = false;
		attribute = collectionAttr;
		return true;
	}
}
