using System.Collections.Generic;
using Sandbox;

namespace RoverDB;

public abstract class Singleton<T> : Component, IHotloadManaged where T : Singleton<T>
{
	public static T Instance { get; private set; } = null!;

	public Singleton()
	{
		Instance = (T)this;
	}

	protected override void OnAwake()
	{
		if ( !Active ) return;
		Instance = (T)this;
	}
	
	protected override void OnDestroy()
	{
		if ( Instance == this )
			Instance = null!;
	}

	void IHotloadManaged.Destroyed( Dictionary<string, object> state )
	{
		state["IsActive"] = Instance == this;
	}

	void IHotloadManaged.Created( IReadOnlyDictionary<string, object> state )
	{
		if ( state.GetValueOrDefault( "IsActive" ) is true )
			Instance = (T)this;
	}
}
