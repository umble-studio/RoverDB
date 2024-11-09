using System;
using System.Collections.Generic;
using System.Linq;
using RoverDB.Helpers;
using Sandbox.Internal;

namespace RoverDB.IO;

internal partial class FileController
{
	/// <summary>
	/// Only let one thread write/read a collection at a time using this lock.
	/// </summary>
	private readonly Dictionary<string, object> _collectionWriteLocks = new();
	
	public void CreateCollectionLock( string collection )
	{
		Log.Info( $"creating collection write lock for collection \"{collection}\"" );

		_collectionWriteLocks[collection] = new object();
	}
	
		/// <summary>
	/// The second return value is null on success, and contains the error message
	/// on failure.
	/// </summary>
	public List<string> ListCollectionNames()
	{
		try
		{
			return _provider.FindDirectory( Config.DatabaseName ).ToList();
		}
		catch ( Exception e )
		{
			Log.Error( "failed to list collection names: " + e.Message );
			return new List<string>();
		}
	}

	/// <summary>
	/// The second return value contains the error message (or null if successful).
	/// </summary>
	public Collection? LoadCollectionDefinition( string collectionName )
	{
		try
		{
			string data;

			if ( !_collectionWriteLocks.ContainsKey( collectionName ) )
				CreateCollectionLock( collectionName );

			lock ( _collectionWriteLocks[collectionName] )
			{
				data = _provider.ReadAllText( $"{Config.DatabaseName}/{collectionName}/definition.txt" );
			}

			if ( string.IsNullOrEmpty( data ) )
			{
				Log.Error( $"no definition.txt for collection \"{collectionName}\" found - see RepairGuide.txt" );
				return null;
			}

			Collection? collection;

			try
			{
				collection = SerializationHelper.Deserialize<Collection>( data );
			}
			catch ( Exception e )
			{
				Log.Error( $"error thrown when deserializing definition.txt for \"{collectionName}\": " +
				           e.StackTrace );
				return null;
			}

			if ( collection?.CollectionName != collectionName )
			{
				Log.Error(
					$"failed to load definition.txt for collection \"{collectionName}\" - the CollectionName in the definition.txt differed from the name of the directory ({collectionName} vs {collection?.CollectionName}) - see RepairGuide.txt" );
				return null;
			}

			try
			{
				collection.DocumentClassType = GlobalGameNamespace.TypeLibrary
					.GetType( collection.DocumentClassTypeSerialized )
					.TargetType;
			}
			catch ( Exception e )
			{
				Log.Error(
					$"couldn't load the type described by the definition.txt for collection \"{collectionName}\" - most probably you renamed or removed your data type - see RepairGuide.txt: " +
					e.StackTrace );
				return null;
			}

			return collection;
		}
		catch ( Exception e )
		{
			Log.Error( $"failed to load definition.txt for collection \"{collectionName}\": " +
			           e.StackTrace );
			return null;
		}
	}
	
	/// <summary>
	/// Returns null on success, or the error message on failure.
	/// </summary>
	public bool SaveCollectionDefinition( Collection collection )
	{
		try
		{
			var data = SerializationHelper.Serialize( collection );

			lock ( _collectionWriteLocks[collection.CollectionName] )
			{
				if ( !_provider.DirectoryExists( $"{Config.DatabaseName}/{collection.CollectionName}" ) )
					_provider.CreateDirectory( $"{Config.DatabaseName}/{collection.CollectionName}" );

				_provider.WriteAllText( $"{Config.DatabaseName}/{collection.CollectionName}/definition.txt", data );
			}

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to save collection definition: " + e.Message );
			return false;
		}
	}
	
	/// <summary>
	/// Returns null on success, or the error message on failure.
	/// </summary>
	private bool DeleteCollection( string name )
	{
		try
		{
			lock ( _collectionWriteLocks[name] )
			{
				_provider.DeleteDirectory( $"{Config.DatabaseName}/{name}", true );
			}

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to delete collection: " + e.Message );
			return false;
		}
	}
}
