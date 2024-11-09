﻿using System.Threading.Tasks;
using Sandbox;

namespace RoverDB;

public sealed partial class RoverDatabase : Singleton<RoverDatabase>
{
	public bool IsInitialised => State is DatabaseState.Initialised;

	protected override async Task OnLoad()
	{
		Log.Info( "Initializing database..." );

		if ( !Networking.IsHost && !Config.ClientsCanUse )
		{
			Log.Error( "only the host can initialize the database - set CLIENTS_CAN_USE to true in Config.cs" +
			           " if you want clients to be able to use the database too" );
			return;
		}

		await GameTask.RunInThreadAsync( Initialize );
	}
}
