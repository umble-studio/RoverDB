using System;
using RoverDB.Testing;

namespace RoverDB.IO;

internal partial class FileController
{
	private IFileIOProvider _provider = null!;

	public Cache.Cache Cache { get; }

	public FileController()
	{
		Cache = new Cache.Cache( this );
	}

	public void Initialize()
	{
		if ( TestHelpers.IsUnitTests )
			_provider = new MockFileIOProvider();
		else
			_provider = new FileIOProvider();
	}
	
	/// <summary>
	/// Wipes all RoverDatabase files. Returns null on success and the error message on failure.
	/// </summary>
	public bool WipeFilesystem()
	{
		try
		{
			var collections = ListCollectionNames();

			if ( collections.Count is not 0 )
			{
				Log.Error( $"failed to wipe filesystem" );
				return false;
			}

			// Don't delete collection folders when we are half-way through writing to them.
			lock ( Cache.WriteInProgressLock )
			{
				foreach ( var collection in collections )
				{
					var delete = DeleteCollection( collection );
					if ( delete ) continue;

					Log.Error( $"failed to wipe filesystem" );
					return false;
				}
			}

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to wipe filesystem: " + e.Message );
			return false;
		}
	}

	/// <summary>
	/// Creates the directories needed for the database. Returns null on success, or the error message
	/// on failure.
	/// </summary>
	public bool EnsureFileSystemSetup()
	{
		try
		{
			if ( !_provider.DirectoryExists( Config.DatabaseName ) )
				_provider.CreateDirectory( Config.DatabaseName );

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to ensure filesystem setup: " + e.Message );
			return false;
		}
	}
}
