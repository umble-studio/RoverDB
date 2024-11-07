using System;
using RoverDB.Attributes;
using RoverDB.Cache;
using RoverDB.CodeGenerators;

namespace RoverDB;

internal static class Shutdown
{
	/// <summary>
	/// S&amp;box doesn't automatically wipe static fields yet so we have to do this
	/// ourselves.
	/// </summary>
	public static void WipeStaticFields()
	{
		Cache.Cache.WipeStaticFields();
		ObjectPool.WipeStaticFields();
		PropertyDescriptionsCache.WipeStaticFields();
		RoverDatabaseAutoSavedEventHandler.WipeStaticFields();
	}

	public static void ShutdownDatabase()
	{
		// Theoretical possibility that the database could be shut down during initialistion,
		// if closed quickly enough.
		lock ( Initialization.InitialisationLock )
		{
			if ( Config.STARTUP_SHUTDOWN_MESSAGES )
			{
				Log.Info( "==================================" );
				Log.Info( "Shutting down RoverDatabase..." );
			}

			try
			{
				if ( Initialization.CurrentDatabaseState == DatabaseState.Initialised )
				{
					// Set this as a matter of priority since if this is wrong on the next start,
					// it won't be wiped automatically by s&box, and there is a risk the user could
					// do an operation on an uninitialised database.
					Initialization.CurrentDatabaseState = DatabaseState.Uninitialised;

					Log.Info( "shutting down database..." );

					Cache.Cache.ForceFullWrite();
					WipeStaticFields();
				}

				// Maybe it was in an irrecoverable error state. Let's just set it back.
				Initialization.CurrentDatabaseState = DatabaseState.Uninitialised;
			}
			catch ( Exception e )
			{
				Initialization.CurrentDatabaseState = DatabaseState.Uninitialised;
				Log.Error( $"failed to shutdown database properly - some data may have been lost: {e.StackTrace}" );
			}

			if ( Config.STARTUP_SHUTDOWN_MESSAGES )
			{
				Log.Info( "Shutdown completed" );
				Log.Info( "==================================" );
			}
		}
	}
}
