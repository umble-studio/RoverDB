using System;
using RoverDB.IO;

namespace RoverDB;

public partial class RoverDatabase
{
	public DatabaseState State { get; private set; }

	/// <summary>
	/// Only let one thread initialse the database at once.
	/// </summary>
	private readonly object _initializationLock = new();

	private void Initialize()
	{
		lock ( _initializationLock )
		{
			if ( State is not DatabaseState.Uninitialised )
				return; // Probably another thread already did all this.

			if ( !Config.MergeJson )
				Log.Warning(
					"Config.MERGE_JSON is set to false - this will delete data if you rename or remove a data field" );

			if ( Config.StartupShutdownMessages )
			{
				Log.Info( "==================================" );
				Log.Info( "Initializing RoverDatabase..." );
			}

			try
			{
				_fileController.Initialise();
				_fileController.EnsureFileSystemSetup();
				
				LoadCollections();
				InitializeTicker();

				State = DatabaseState.Initialised;

				if ( Config.StartupShutdownMessages )
				{
					Log.Info( "RoverDatabase initialisation finished successfully" );
					Log.Info( "==================================" );
				}
			}
			catch ( Exception e )
			{
				Log.Error( $"failed to initialise database: {e.StackTrace}" );

				if ( Config.StartupShutdownMessages )
				{
					Log.Info( "RoverDatabase initialisation finished unsuccessfully" );
					Log.Info( "==================================" );
				}
			}
		}
	}

	private void LoadCollections()
	{
		var collectionNames = _fileController.ListCollectionNames();

		foreach ( var collectionName in collectionNames )
		{
			Log.Info( $"attempting to load collection \"{collectionName}\"" );
			LoadCollection( collectionName );
		}
	}

	/// <summary>
	/// Returns null on success or the error message on failure.
	/// </summary>
	private bool LoadCollection( string name )
	{
		var definition = _fileController.LoadCollectionDefinition( name );

		if ( definition is null )
		{
			Log.Error(
				$"found a folder for collection {name} but the definition.txt was missing in that folder or failed to load" );
			return false;
		}

		var documents = _fileController.LoadAllCollectionsDocuments( definition );

		_fileController.Cache.CreateCollection( name, definition.DocumentClassType );
		_fileController.Cache.InsertDocumentsIntoCollection( name, documents );

		Log.Info( $"Loaded collection {name} with {documents.Count} documents" );
		return true;
	}
}
