using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace RoverDB;

public partial class RoverDatabase
{
	private readonly ConcurrentDictionary<string, Collection> _collections = new();

	[Property, ReadOnly] public List<Collection> Collections => _collections.Values.ToList();

	private Collection? GetCollectionByName( string name )
	{
		return _collections.GetValueOrDefault( name );
	}

	private Collection CreateCollection( string name )
	{
		var collection = new Collection( name );
		_collections[name] = collection;

		return collection;
	}
}
