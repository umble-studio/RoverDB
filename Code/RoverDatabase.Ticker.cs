using System.Threading.Tasks;
using RoverDB.Cache;
using RoverDB.Testing;
using Sandbox;

namespace RoverDB;

public partial class RoverDatabase
{
	internal void InitializeTicker()
	{
		GameTask.RunInThreadAsync( async () =>
		{
			Log.Info( "Initialising ticker..." );

			while( Game.IsPlaying || TestHelpers.IsUnitTests )
			{
				_fileController.Cache.Tick();
				ObjectPool.TryCheckPool();

				await Task.Delay( Config.TICK_DELTA );
			}
		} );
	}
}
