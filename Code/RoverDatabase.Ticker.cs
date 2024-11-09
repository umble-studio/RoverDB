using RoverDB.Testing;
using Sandbox;

namespace RoverDB;

public partial class RoverDatabase
{
	private void InitializeTicker()
	{
		GameTask.RunInThreadAsync( async () =>
		{
			Log.Info( "Initializing ticker..." );

			while ( Game.IsPlaying || TestHelpers.IsUnitTests )
			{
				// foreach ( var (_, collection) in _collections )
				// 	collection.Save();

				await GameTask.DelaySeconds( Config.SaveInterval );
			}
		} );
	}
}
