using System;
using System.Linq;
using RoverDB.Attributes;
using Sandbox;
using Sandbox.Internal;

namespace RoverDB.Helpers;

internal static class PropertyHelper
{
	public static bool HasPropertyId( object value )
	{
		return GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( value )
			.FirstOrDefault( x => x.Attributes.Any( a => a is IdAttribute ) ) is not null;
	}

	public static PropertyDescription GetPropertyId( object value )
	{
		return GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( value )
			.FirstOrDefault( x => x.Attributes.Any( a => a is IdAttribute ) )!;
	}

	public static bool IsPropertyGuid( this PropertyDescription property )
	{
		return property.PropertyType == typeof( Guid );
	}
}
