using System;
using RoverDB.Extensions;
using Sandbox;

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
				CachePropertyExtensions.Wipe();

				if ( !EnsureFileSystemSetup() ) return;

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
		lock ( _initializationLock )
		{
			var collectionNames = Collection.ListCollectionNames();
			Log.Warning( $"Loading {collectionNames.Count} collections" );

			foreach ( var collectionName in collectionNames )
			{
				Log.Info( $"attempting to load collection \"{collectionName}\"" );

				var collection = new Collection( collectionName );
				collection.Load();
				
				_collections[collectionName] = collection;
			}
		}
	}

	private static bool EnsureFileSystemSetup()
	{
		try
		{
			if ( !FileSystem.Data.DirectoryExists( Config.DatabaseName ) )
				FileSystem.Data.CreateDirectory( Config.DatabaseName );

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to ensure filesystem setup: " + e.Message );
			return false;
		}
	}
}
