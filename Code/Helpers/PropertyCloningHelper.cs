using RoverDB.Attributes;
using RoverDB.Cache;
using Sandbox;

namespace RoverDB.Helpers;

internal static class PropertyCloningHelper
{
	public static void CopyClassData<T>( T sourceClass, T destinationClass )
	{
		// This is probably not much faster since we still have to call GetType, but oh well.
		var properties = PropertyDescriptionsCache.GetPropertyDescriptionsForType( sourceClass.GetType().FullName,
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

		// Copy the UID right at the end as this will prevent update spam if the class uses AutoSave properties.
		uidProperty?.SetValue( destinationClass, uidProperty.GetValue( sourceClass ) );
	}

	public static void CopyClassData( object sourceClass, object destinationClass, string classTypeName )
	{
		var properties = PropertyDescriptionsCache.GetPropertyDescriptionsForType( sourceClass.GetType().FullName!, sourceClass );
		// var properties = PropertyDescriptionsCache.GetPropertyDescriptionsForType( classTypeName, sourceClass );

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

		// Copy the UID right at the end as this will prevent update spam if the class uses AutoSave properties.
		uidProperty?.SetValue( destinationClass, uidProperty.GetValue( sourceClass ) );
	}
}
