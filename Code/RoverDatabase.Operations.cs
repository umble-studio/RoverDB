using System;
using System.Collections.Generic;
using System.Linq;
using RoverDB.Extensions;
using RoverDB.Helpers;

namespace RoverDB;

public partial class RoverDatabase
{
	public bool Insert<T>( T document ) where T : class
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return false;

		try
		{
			// The collection does not exist, create it
			var relevantCollection = GetCollectionByName( collectionAttr.Name )
			                         ?? CreateCollection( collectionAttr.Name );

			var newDocument = new Document( document, collectionAttr.Name );
			relevantCollection.InsertDocument( newDocument );

			return true;
		}
		catch ( Exception ex )
		{
			Log.Error( "failed to insert document: " + ex.Message );
			return false;
		}
	}

	public bool InsertMany<T>( IEnumerable<T> documents ) where T : class
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return false;

		try
		{
			// The collection does not exist, create it
			var relevantCollection = GetCollectionByName( collectionAttr.Name )
			                         ?? CreateCollection( collectionAttr.Name );

			foreach ( var document in documents )
			{
				var newDocument = new Document( document, collectionAttr.Name );
				relevantCollection.InsertDocument( newDocument );
			}

			return true;
		}
		catch ( Exception ex )
		{
			Log.Error( "failed to insert many documents: " + ex.StackTrace );
			return false;
		}
	}

	public T? SelectOne<T>( Func<T, bool> selector ) where T : class, new()
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return null;

		var relevantCollection = GetCollectionByName( collectionAttr.Name );

		if ( relevantCollection is null )
			return null;

		foreach ( var pair in relevantCollection.Documents )
		{
			if ( !selector.Invoke( (T)pair.Value.Data ) ) continue;
			return (T)pair.Value.Data;
		}

		return null;
	}

	public List<T> Select<T>( Func<T, bool>? selector = null ) where T : class
	{
		var output = new List<T>();

		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return output;

		var relevantCollection = GetCollectionByName( collectionAttr.Name );

		if ( relevantCollection is null )
			return output;

		Log.Info( "SELECTING FROM " +
		          string.Join( ", ", relevantCollection.Name, relevantCollection.Documents.Count ) );

		foreach ( var pair in relevantCollection.Documents )
		{
			Log.Info( "Pair: " + string.Join( ", ", pair.Value.Data.GetType(), typeof(T) ) );
			
			// Collection can have multiple types of documents in it
			// Ignore documents that are not of type T
			if ( pair.Value.Data.GetType() != typeof(T) )
				continue;

			if ( selector is null || selector.Invoke( (T)pair.Value.Data ) )
			{
				// output.Add(
				// 	CachePropertyExtensions.CloneObject( (T)pair.Value.Data ) );

				// TODO - Maybe need to clone the object ?
				output.Add( (T)pair.Value.Data );
			}
		}

		return output;
	}

	public bool Delete<T>( Predicate<T> selector ) where T : class
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return false;

		var relevantCollection = GetCollectionByName( collectionAttr.Name );

		if ( relevantCollection is null )
			return false;

		foreach ( var pair in relevantCollection.Documents )
		{
			if ( !selector.Invoke( (T)pair.Value.Data ) )
				continue;

			relevantCollection.DeleteDocument( pair.Value.DocumentId );
		}

		return true;
	}

	public bool Any<T>( Func<T, bool> selector ) where T : class
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return false;

		var relevantCollection = GetCollectionByName( collectionAttr.Name );

		if ( relevantCollection is null )
		{
			Log.Error( "failed to select any document from collection: collection not found" );
			return false;
		}

		foreach ( var pair in relevantCollection.Documents )
		{
			if ( selector.Invoke( (T)pair.Value.Data ) )
				return true;
		}

		return false;
	}

	public bool Exists<T>( Func<T, bool> selector ) where T : class
	{
		return Select( selector ).FirstOrDefault( selector ) is not null;
	}
}
