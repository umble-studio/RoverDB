using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace RoverDB;

public partial class RoverDatabase
{
	private readonly Dictionary<string, Collection> _collections = new();

	[Property, ReadOnly] public List<Collection> Collections => _collections.Values.ToList();

	private Collection? GetCollectionByName( string name )
	{
		return CollectionExtensions.GetValueOrDefault( _collections, name );
	}

	private Collection CreateCollection( string name )
	{
		var collection = new Collection( name );
		_collections.Add( name, collection );

		return collection;
	}
}
