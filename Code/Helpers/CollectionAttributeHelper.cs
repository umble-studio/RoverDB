using System;
using RoverDB.Attributes;
using Sandbox;

namespace RoverDB.Helpers;

public static class CollectionAttributeHelper
{
	public static Type? GetCollectionType( this Type type )
	{
		var t = TypeLibrary.GetType( type );
		var collectionAttr = t.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
			return t.TargetType;

		collectionAttr = t.BaseType.GetAttribute<CollectionAttribute>();
		return collectionAttr is not null ? t.BaseType.TargetType : null;
	}

	public static bool TryGetAttribute( TypeDescription type, out CollectionAttribute attribute )
	{
		var t = type;
		var collectionAttr = t.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is null )
		{
			attribute = null!;
			return false;
		}

		t = type.BaseType;

		// Check if its ancestor have the collection attribute
		collectionAttr = t.GetAttribute<CollectionAttribute>();
		Log.Info( "BaseType: " + t.Name );

		if ( collectionAttr is not null )
		{
			attribute = collectionAttr;
			return true;
		}

		Log.Error( $"Type {t.FullName} is not a collection" );

		attribute = null!;
		return true;
	}
}
