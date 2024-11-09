using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RoverDB;

public partial class RoverDatabase
{
	private readonly ConcurrentDictionary<string, Collection> _collections = new();

	private Collection? GetCollectionByName( string name )
	{
		return _collections.GetValueOrDefault( name );
	}

	private Collection CreateCollection( string name )
	{
		var collection = new Collection { Name = name };
		collection.SaveDefinition();
		
		_collections[name] = collection;
		return collection;
	}
}
