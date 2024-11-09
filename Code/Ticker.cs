using System.Threading.Tasks;
using RoverDB.Cache;
using RoverDB.Testing;
using Sandbox;

namespace RoverDB;

internal static class Ticker
{
	public static void Initialise()
	{
		GameTask.RunInThreadAsync( async () =>
		{
			Log.Info( "Initialising ticker..." );

			while( Game.IsPlaying || TestHelpers.IsUnitTests )
			{
				Cache.Cache.Tick();
				ObjectPool.TryCheckPool();

				await Task.Delay( Config.TICK_DELTA );
			}
		} );
	}
}
