using System.Linq;
using RoverDB.Attributes;
using RoverDB.Exceptions;
using Sandbox;
using Sandbox.Internal;

namespace RoverDB.CodeGenerators;

public static class RoverDatabaseAutoSavedEventHandler
{
	private static readonly object _autoSaveLock = new();
	private static object? _objectBeingAutoSaved;

	public static void WipeStaticFields()
	{
		_objectBeingAutoSaved = null;
	}

	public static void AutoSave<T>( WrappedPropertySet<T> p )
	{
		p.Setter( p.Value );

		// Don't auto-save while we are initialising. It is pointless.
		if ( !RoverDatabase.IsInitialised ) return;

		// var id = (string)GlobalGameNamespace.TypeLibrary.GetPropertyValue( p.Object, "UID" );
		//
		// // If the UID is not set then we can assume this document hasn't even been fully created yet.
		// if ( string.IsNullOrEmpty( id ) ) return;

		var propertyId = GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( p.Object )
			.FirstOrDefault( x => x.Attributes.Any( a => a is IdAttribute ) );

		if ( propertyId is null )
		{
			throw new RoverDatabaseException(
				"Cannot handle a document without a property marked with a Id attribute - make sure your data class has a public property called UID, like this: \"[Saved] public string UID { get; set; }\"" );
		}

		var propertyValue = propertyId.GetValue( p.Object );
		if ( propertyValue is null || string.IsNullOrEmpty( propertyValue?.ToString() ) ) return;

		lock ( _autoSaveLock )
		{
			// When we save this in a moment, the cache will create a copy of it. This will basically send it right
			// back here. So to avoid an infinite loop, don't auto save if we're already auto saving an object.
			//
			// This means that only one object can be auto saved at a time, but in practice this isn't really a big
			// deal since a) 95% of users won't be saving things in multiple threads and b) auto save is meant for
			// people who don't care about performance. Also, creating a system for locking each object could just
			// end up adding more latency than it's worth.
			if ( _objectBeingAutoSaved is not null ) return;

			try
			{
				_objectBeingAutoSaved = p.Object;

				var collectionName = (string)GlobalGameNamespace.TypeLibrary.GetPropertyValue(
					p.Attributes.First( x => x.GetType().ToString() == "RoverDB.Attributes.AutoSavedAttribute" ),
					"CollectionName" );

				RoverDatabase.Insert( collectionName, p.Object, p.Object.GetType() );
			}
			finally
			{
				_objectBeingAutoSaved = null;
			}
		}
	}
}
