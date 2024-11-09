using RoverDB.Attributes;
using Sandbox;

namespace RoverDB.Extensions;

internal static class CachePropertyExtensions
{
	public static void CopyClassData<T>( this Cache.Cache cache, T sourceClass, T destinationClass )
	{
		// This is probably not much faster since we still have to call GetType, but oh well.
		var properties = cache.GetPropertyDescriptionsForType( sourceClass.GetType().FullName,
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

	public static void CopyClassData( this Cache.Cache cache, object sourceClass, object destinationClass, string classTypeName )
	{
		var properties = cache.GetPropertyDescriptionsForType( sourceClass.GetType().FullName!, sourceClass );
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
