using System;
using RoverDB.IO;

namespace RoverDB;

internal static class Initialization
{
	public static DatabaseState CurrentDatabaseState;

	/// <summary>
	/// Only let one thread initialse the database at once.
	/// </summary>
	public static readonly object InitialisationLock = new();

	public static void Initialize()
	{
		lock ( InitialisationLock )
		{
			if ( CurrentDatabaseState is not DatabaseState.Uninitialised )
				return; // Probably another thread already did all this.

			if ( !Config.MERGE_JSON )
				Log.Warning(
					"Config.MERGE_JSON is set to false - this will delete data if you rename or remove a data field" );

			if ( Config.STARTUP_SHUTDOWN_MESSAGES )
			{
				Log.Info( "==================================" );
				Log.Info( "Initializing RoverDatabase..." );
			}

			try
			{
				FileController.Initialise();
				FileController.EnsureFileSystemSetup();
				LoadCollections();
				Ticker.Initialise();

				CurrentDatabaseState = DatabaseState.Initialised;

				if ( Config.STARTUP_SHUTDOWN_MESSAGES )
				{
					Log.Info( "RoverDatabase initialisation finished successfully" );
					Log.Info( "==================================" );
				}
			}
			catch ( Exception e )
			{
				Log.Error( $"failed to initialise database: {e.StackTrace}" );

				if ( Config.STARTUP_SHUTDOWN_MESSAGES )
				{
					Log.Info( "RoverDatabase initialisation finished unsuccessfully" );
					Log.Info( "==================================" );
				}
			}
		}
	}

	private static void LoadCollections()
	{
		var collectionNames = FileController.ListCollectionNames();

		foreach ( var collectionName in collectionNames )
		{
			Log.Info( $"attempting to load collection \"{collectionName}\"" );
			LoadCollection( collectionName );
		}
	}

	/// <summary>
	/// Returns null on success or the error message on failure.
	/// </summary>
	private static bool LoadCollection( string name )
	{
		var definition = FileController.LoadCollectionDefinition( name );

		if ( definition is null )
		{
			Log.Error(
				$"found a folder for collection {name} but the definition.txt was missing in that folder or failed to load" );
			return false;
		}

		var documents = FileController.LoadAllCollectionsDocuments( definition );

		Cache.Cache.CreateCollection( name, definition.DocumentClassType );
		Cache.Cache.InsertDocumentsIntoCollection( name, documents );

		Log.Info( $"Loaded collection {name} with {documents.Count} documents" );
		return true;
	}
}
