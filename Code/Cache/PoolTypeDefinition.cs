using System;
using System.Collections.Concurrent;

namespace RoverDB.Cache;

internal readonly struct PoolTypeDefinition
{
	public readonly Type ObjectType;
	public readonly ConcurrentBag<object> TypePool;

	public PoolTypeDefinition( Type objectType, ConcurrentBag<object> typePool )
	{
		ObjectType = objectType;
		TypePool = typePool;
	}
}
