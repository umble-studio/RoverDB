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

			while( Game.IsPlaying || TestHelpers.IsUnitTests )
			{
				_fileController.Cache.Tick();
				_fileController.Cache.Pool.TryCheckPool();

				await Task.Delay( Config.TickDelta );
			}
		} );
	}
}
