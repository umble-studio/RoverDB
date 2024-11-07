using System;
using System.Collections.Generic;
using RoverDB.Exceptions;
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
				Shutdown.WipeStaticFields();
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
		var attempt = 0;
		string? error = null;
		List<string>? collectionNames;

		while ( true )
		{
			if ( attempt++ >= 10 )
				throw new RoverDatabaseException( $"failed to load collection list after 10 tries: {error}" );

			(collectionNames, error) = FileController.ListCollectionNames();

			if ( string.IsNullOrEmpty( error ) )
				break;
		}

		attempt = 0;

		if ( collectionNames is null ) return;
		
		foreach ( var collectionName in collectionNames )
		{
			Log.Info( $"attempting to load collection \"{collectionName}\"" );

			while ( true )
			{
				if ( attempt++ >= 10 )
					throw new RoverDatabaseException(
						$"failed to load collection {collectionName} after 10 tries: {error}" );

				error = LoadCollection( collectionName );

				if ( string.IsNullOrEmpty( error ) )
					break;
			}
		}
	}

	/// <summary>
	/// Returns null on success or the error message on failure.
	/// </summary>
	private static string? LoadCollection( string name )
	{
		var (definition, error) = FileController.LoadCollectionDefinition( name );

		if ( !string.IsNullOrEmpty( error ) )
			return $"failed loading collection definition for collection \"{name}\": {error}";

		if ( definition is null )
			return
				$"found a folder for collection {name} but the definition.txt was missing in that folder or failed to load";

		(var documents, error) = FileController.LoadAllCollectionsDocuments( definition );
		
		if ( documents is null ) 
			return null;
		
		if ( !string.IsNullOrEmpty( error ) )
			return $"failed loading documents for collection \"{name}\": {error}";

		Cache.Cache.CreateCollection( name, definition.DocumentClassType );
		Cache.Cache.InsertDocumentsIntoCollection( name, documents );

		Log.Info( $"Loaded collection {name} with {documents.Count} documents" );

		return null;
	}
}
