using System.Threading.Tasks;
using RoverDB.IO;
using Sandbox;

namespace RoverDB;

public sealed partial class RoverDatabase : Singleton<RoverDatabase>
{
	private readonly FileController _fileController = new();

	public bool IsInitialised => State is DatabaseState.Initialised;

	/// <summary>
	/// Initialises the database. You don't have to call this manually as the database will do this for you
	/// when you make your first request. However, you may want to call this manually when the server starts
	/// if your database is particularly big, to avoid the game freezing when the first request is made. Example:
	/// <br/><br/>
	/// <strong>await RoverDatabase.InitialiseAsync()</strong>
	/// <br/>
	/// or
	/// <br/>
	/// <strong>RoverDatabase.InitialiseAsync().GetAwaiter().GetResult()</strong>
	/// <br/><br/>
	/// It is perfectly safe to call this function many times from many different places; the database will only
	/// be initialised once.
	/// </summary>
	public async Task InitializeAsync()
	{
		if ( !Networking.IsHost && !Config.CLIENTS_CAN_USE )
		{
			Log.Error( "only the host can initialise the database - set CLIENTS_CAN_USE to true in Config.cs" +
			           " if you want clients to be able to use the database too" );
			return;
		}

		await GameTask.RunInThreadAsync( Initialize );
	}

	protected override async Task OnLoad()
	{
		if ( !Networking.IsHost && !Config.CLIENTS_CAN_USE )
		{
			Log.Error( "only the host can initialise the database - set CLIENTS_CAN_USE to true in Config.cs" +
			           " if you want clients to be able to use the database too" );
			return;
		}

		await GameTask.RunInThreadAsync( Initialize );
	}

	public void Shutdown()
	{
	}
}
