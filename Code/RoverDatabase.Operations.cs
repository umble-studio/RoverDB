using System;
using System.Collections.Generic;
using System.Linq;
using RoverDB.Extensions;
using RoverDB.Helpers;
using Sandbox.Internal;

namespace RoverDB;

public partial class RoverDatabase
{
	/// <summary>
	/// Copy the saveable data from one class to another. This is useful for when you load
	/// data from the database and you want to put it in a component or something like that.
	/// </summary>
	internal void CopySavedData<T>( T sourceClass, T destinationClass )
	{
		_fileController.Cache.CopyClassData( sourceClass, destinationClass );
	}

	/// <summary>
	/// Insert a document into the database. The document will have its ID set
	/// if it is empty.
	/// </summary>
	public bool Insert<T>( T document ) where T : class
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return false;

		var relevantCollection = _fileController.Cache.GetCollectionByName<T>( collectionAttr.Name, true );

		if ( relevantCollection is null )
		{
			Log.Error( "failed to insert document into collection: collection not found" );
			return false;
		}

		var newDocument = new Document( document, collectionAttr.Name );
		relevantCollection.InsertDocument( _fileController, newDocument );

		return true;
	}

	/// <summary>
	/// Insert multiple documents into the database. The documents will have their IDs
	/// set if they are empty.
	/// </summary>
	public bool InsertMany<T>( IEnumerable<T> documents ) where T : class
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) ) 
			return false;

		var relevantCollection = _fileController.Cache.GetCollectionByName<T>( collectionAttr.Name, true );

		if ( relevantCollection is null )
		{
			Log.Error( "failed to insert multiple documents into collection: collection not found" );
			return false;
		}

		foreach ( var document in documents )
		{
			var newDocument = new Document( document, collectionAttr.Name );
			relevantCollection.InsertDocument( _fileController, newDocument );
		}

		return true;
	}

	/// <summary>
	/// Fetch a single document from the database where selector evaluates to true.
	/// </summary>
	public T? SelectOne<T>( Func<T, bool> selector ) where T : class, new()
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return null;

		var relevantCollection = _fileController.Cache.GetCollectionByName<T>( collectionAttr.Name, false );

		if ( relevantCollection is null )
			return null;

		foreach ( var pair in relevantCollection.Documents )
		{
			if ( selector.Invoke( (T)pair.Value.Data ) )
				return _fileController.Cache.Pool.CloneObject( (T)pair.Value.Data,
					relevantCollection.DocumentClassType.FullName );
		}

		return null;
	}

	/// <summary>
	/// Select all documents from the database where selector evaluates to true.
	/// </summary>
	public List<T> Select<T>( Func<T, bool>? selector = null ) where T : class
	{
		var output = new List<T>();

		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return output;

		var relevantCollection = _fileController.Cache.GetCollectionByName<T>( collectionAttr.Name, false );

		if ( relevantCollection is null )
			return output;

		foreach ( var pair in relevantCollection.Documents )
		{
			// If the current document is not of the correct type, ignore it.
			if ( pair.Value.Data.GetType() != typeof(T) )
				continue;

			if ( selector is null || selector.Invoke( (T)pair.Value.Data ) )
			{
				output.Add(
					_fileController.Cache.Pool.CloneObject( (T)pair.Value.Data,
						relevantCollection.DocumentClassType.FullName ) );
			}
		}

		return output;
	}

	/// <summary>
	/// Delete all documents from the database where selector evaluates to true.
	/// </summary>
	public bool Delete<T>( Predicate<T> selector ) where T : class
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) ) 
			return false;

		var relevantCollection = _fileController.Cache.GetCollectionByName<T>( collectionAttr.Name, false );

		if ( relevantCollection is null )
			return false;

		var idsToDelete = new List<object>();

		foreach ( var pair in relevantCollection.Documents )
		{
			if ( selector.Invoke( (T)pair.Value.Data ) )
				idsToDelete.Add( pair.Key );
		}

		foreach ( var id in idsToDelete )
		{
			relevantCollection.Documents.TryRemove( id, out _ );
			_fileController.DeleteDocument( collectionAttr.Name, id );
		}

		return true;
	}

	/// <summary>
	/// Return whether there are any documents in the database where selector evaluates
	/// to true.
	/// </summary>
	public bool Any<T>( Func<T, bool> selector ) where T : class
	{
		if ( !CollectionAttributeHelper.TryGetAttribute( typeof(T), out var collectionAttr ) )
			return false;

		var relevantCollection = _fileController.Cache.GetCollectionByName<T>( collectionAttr.Name, false );

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

	/// <summary>
	/// Return whether there are any documents in the database where selector evaluates
	/// to true.
	/// </summary>
	public bool Exists<T>( Func<T, bool> selector ) where T : class
	{
		var result = Select( selector ).FirstOrDefault( selector );
		return result is not null;
	}

	/// <summary>
	/// Deletes everything, forever.
	/// </summary>
	public void DeleteAllData()
	{
		_fileController.WipeFilesystem();
	}
}
