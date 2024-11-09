using System;
using System.Collections.Generic;
using System.Linq;
using RoverDB.Cache;
using RoverDB.Helpers;
using Sandbox.Internal;

namespace RoverDB;

public partial class RoverDatabase
{
	/// <summary>
	/// Copy the saveable data from one class to another. This is useful for when you load
	/// data from the database and you want to put it in a component or something like that.
	/// </summary>
	internal static void CopySavedData<T>( T sourceClass, T destinationClass )
	{
		PropertyCloningHelper.CopyClassData( sourceClass, destinationClass );
	}
	
		/// <summary>
	/// Insert a document into the database. The document will have its ID set
	/// if it is empty.
	/// </summary>
	public void Insert<T>( T document ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = GlobalGameNamespace.TypeLibrary.GetType<T>();
		if ( !CollectionAttributeHelper.TryGetAttribute( type, out _, out var collectionAttr ) ) return;

		var relevantCollection = _cache.GetCollectionByName<T>( collectionAttr.Name, true );

		var newDocument = new Document( document, true, collectionAttr.Name );
		relevantCollection.InsertDocument( newDocument );
	}

	/// <summary>
	/// Insert a document into the database. The document will have its ID set if it is empty.
	/// 
	/// For internal use.
	/// </summary>
	internal void Insert( string collection, object document )
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = _cache.GetCollectionByName( collection, true, document.GetType() );

		var newDocument = new Document( document, true, collection );
		relevantCollection.InsertDocument( newDocument );
	}

	/// <summary>
	/// Insert multiple documents into the database. The documents will have their IDs
	/// set if they are empty.
	/// </summary>
	public void InsertMany<T>( IEnumerable<T> documents ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = GlobalGameNamespace.TypeLibrary.GetType<T>();
		if ( !CollectionAttributeHelper.TryGetAttribute( type, out _, out var collectionAttr ) ) return;

		if ( collectionAttr is null )
			return;

		var relevantCollection = _cache.GetCollectionByName<T>( collectionAttr.Name, true );

		foreach ( var document in documents )
		{
			var newDocument = new Document( document, true, collectionAttr.Name );
			relevantCollection.InsertDocument( newDocument );
		}
	}

	/// <summary>
	/// Fetch a single document from the database where selector evaluates to true.
	/// </summary>
	public T? SelectOne<T>( Func<T, bool> selector ) where T : class, new()
	{
		var type = GlobalGameNamespace.TypeLibrary.GetType<T>();

		if ( !CollectionAttributeHelper.TryGetAttribute( type, out _, out var collectionAttr ) )
			return null;

		if ( collectionAttr is null )
			return null;

		var relevantCollection = _cache.GetCollectionByName<T>( collectionAttr.Name, false );

		if ( relevantCollection is null )
			return null;

		foreach ( var pair in relevantCollection.CachedDocuments )
		{
			if ( selector.Invoke( (T)pair.Value.Data ) )
				return ObjectPool.CloneObject( (T)pair.Value.Data, relevantCollection.DocumentClassType.FullName );
		}

		return null;
	}

	/// <summary>
	/// Select all documents from the database where selector evaluates to true.
	/// </summary>
	public List<T> Select<T>( Func<T, bool>? selector = null ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = GlobalGameNamespace.TypeLibrary.GetType<T>();
		var output = new List<T>();

		Log.Info("Selecting documents... " + typeof(T));
		
		if ( !CollectionAttributeHelper.TryGetAttribute( type, out _, out var collectionAttr ) )
			return output;

		if ( collectionAttr is null )
			return output;

		var relevantCollection = _cache.GetCollectionByName<T>( collectionAttr.Name, false );

		if ( relevantCollection is null )
			return output;

		foreach ( var pair in relevantCollection.CachedDocuments )
		{
			// If the current document is not of the correct type, ignore it.
			if ( pair.Value.Data.GetType() != typeof(T) )
				continue;

			if ( selector is null || selector.Invoke( (T)pair.Value.Data ) )
			{
				output.Add(
					ObjectPool.CloneObject( (T)pair.Value.Data, relevantCollection.DocumentClassType.FullName ) );
			}
		}

		return output;
	}

	/// <summary>
	/// DO NOT USE THIS FUNCTION UNLESS YOU FULLY UNDERSTAND THE BELOW, AS THERE IS
	/// A RISK YOU COULD CORRUPT YOUR DATA. <br/>
	/// <br/>
	/// This does the exact same thing as Select, except it is about 9x faster.
	/// They work differently, however. <br/>
	/// <br/>
	/// Select copies the data from the cache into new objects and then gives those
	/// new objects to you. That means that any changes you make to those new objects
	/// don't affect anything else - you're free to do what you want with them. The
	/// downside to this is that there is an overhead invovled in creating all those
	/// new objects. <br/>
	/// <br/>
	/// SelectUnsafeReferences on the other hand will give you a reference to the data
	/// that is stored in the cache. This is faster because it means no new copy has to
	/// be made. However, because it's giving you a reference, this means that ANY CHANGES
	/// YOU MAKE TO THE RETURNED OBJECTS WILL BE REFLECTED IN THE CACHE, AND THEREFORE MAY
	/// CHANGE THE VALUES IN THE DATABASE UNEXEPECTEDLY!!! You should therefore not modify
	/// the returned objects in any way, only read them.<br/>
	/// <br/>
	/// You are guaranteed that the cache will not change the object after you have requested
	/// it (because all inserts are new objects).
	/// </summary>
	public List<T> SelectUnsafeReferences<T>( Func<T, bool> selector ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var output = new List<T>();
		var type = GlobalGameNamespace.TypeLibrary.GetType<T>();

		if ( !CollectionAttributeHelper.TryGetAttribute( type, out _, out var collectionAttr ) )
			return output;

		if ( collectionAttr is null )
			return output;

		var relevantCollection = _cache.GetCollectionByName<T>( collectionAttr.Name, false );

		if ( relevantCollection is null )
			return output;

		foreach ( var pair in relevantCollection.CachedDocuments )
		{
			if ( selector.Invoke( (T)pair.Value.Data ) )
				output.Add( (T)pair.Value.Data );
		}

		return output;
	}

	/// <summary>
	/// Delete all documents from the database where selector evaluates to true.
	/// </summary>
	public void Delete<T>( Predicate<T> selector ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = GlobalGameNamespace.TypeLibrary.GetType<T>();
		if ( !CollectionAttributeHelper.TryGetAttribute( type, out _, out var collectionAttr ) ) return;

		if ( collectionAttr is null )
			return;

		var relevantCollection = _cache.GetCollectionByName<T>( collectionAttr.Name, false );
		if ( relevantCollection is null ) return;

		var idsToDelete = new List<object>();

		foreach ( var pair in relevantCollection.CachedDocuments )
		{
			if ( selector.Invoke( (T)pair.Value.Data ) )
				idsToDelete.Add( pair.Key );
		}

		foreach ( var id in idsToDelete )
		{
			relevantCollection.CachedDocuments.TryRemove( id, out _ );
			_fileController.DeleteDocument( collectionAttr.Name, id );
		}
	}

	/// <summary>
	/// Return whether there are any documents in the database where selector evaluates
	/// to true.
	/// </summary>
	public bool Any<T>( Func<T, bool> selector ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = GlobalGameNamespace.TypeLibrary.GetType<T>();

		if ( !CollectionAttributeHelper.TryGetAttribute( type, out _, out var collectionAttr ) )
			return false;

		var relevantCollection = _cache.GetCollectionByName<T>( collectionAttr.Name, false );

		if ( relevantCollection is null )
			return false;

		foreach ( var pair in relevantCollection.CachedDocuments )
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
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var result = Select( selector ).FirstOrDefault( selector );
		return result is not null;
	}

	/// <summary>
	/// Deletes everything, forever.
	/// </summary>
	public void DeleteAllData()
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		// _fileController.Cache.WipeStaticFields();
		_fileController.WipeFilesystem();
	}
}
