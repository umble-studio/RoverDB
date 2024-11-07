using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RoverDB.Attributes;
using RoverDB.Cache;
using RoverDB.Exceptions;
using RoverDB.Helpers;
using RoverDB.IO;
using Sandbox;

namespace RoverDB;

public static class RoverDatabase
{
	public static bool IsInitialised => Initialization.CurrentDatabaseState == DatabaseState.Initialised;

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
	public static async Task InitializeAsync()
	{
		if ( !Networking.IsHost && !Config.CLIENTS_CAN_USE )
		{
			Log.Error( "only the host can initialise the database - set CLIENTS_CAN_USE to true in Config.cs" +
			           " if you want clients to be able to use the database too" );
			return;
		}

		await GameTask.RunInThreadAsync( Initialization.Initialize );
	}

	/// <summary>
	/// Copy the saveable data from one class to another. This is useful for when you load
	/// data from the database and you want to put it in a component or something like that.
	/// </summary>
	public static void CopySavedData<T>( T sourceClass, T destinationClass )
	{
		Cloning.CopyClassData<T>( sourceClass, destinationClass );
	}

	/// <summary>
	/// Insert a document into the database. The document will have its ID set
	/// if it is empty.
	/// </summary>
	public static void Insert<T>( string collection, T document ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, true );

		var newDocument = new Document( document, typeof(T), true, collection );
		relevantCollection.InsertDocument( newDocument );
	}

	/// <summary>
	/// Insert a document into the database. The document will have its ID set
	/// if it is empty.
	/// </summary>
	public static void Insert<T>( T document ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
		{
			Insert( collectionAttr.Name, document );
			return;
		}

		Log.Error( $"Type {type.FullName} is not a collection" );
	}

	/// <summary>
	/// Insert a document into the database. The document will have its ID set if it is empty.
	/// 
	/// For internal use.
	/// </summary>
	internal static void Insert( string collection, object document, Type documentType )
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName( collection, true, documentType );

		Document newDocument = new(document, documentType, true, collection);
		relevantCollection.InsertDocument( newDocument );
	}

	/// <summary>
	/// Insert multiple documents into the database. The documents will have their IDs
	/// set if they are empty.
	/// </summary>
	public static void InsertMany<T>( string collection, IEnumerable<T> documents ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, true );

		foreach ( var document in documents )
		{
			var newDocument = new Document( document, typeof(T), true, collection );
			relevantCollection.InsertDocument( newDocument );
		}
	}

	/// <summary>
	/// Insert multiple documents into the database. The documents will have their IDs
	/// set if they are empty.
	/// </summary>
	public static void InsertMany<T>( IEnumerable<T> documents ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
		{
			InsertMany( collectionAttr.Name, documents );
			return;
		}

		Log.Error( $"Type {type.FullName} is not a collection" );
	}

	/// <summary>
	/// Fetch a single document from the database where selector evaluates to true.
	/// </summary>
	public static T? SelectOne<T>( string collection, Func<T, bool> selector ) where T : class, new()
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, false );

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
	/// Fetch a single document from the database where selector evaluates to true.
	/// </summary>
	public static T? SelectOne<T>( Func<T, bool> selector ) where T : class, new()
	{
		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
			return SelectOne( collectionAttr.Name, selector );

		Log.Error( $"Type {type.FullName} is not a collection" );
		return null;
	}

	/// <summary>
	/// The same as SelectOne except faster since we can look it up by ID.
	/// </summary>
	public static T? SelectOneWithID<T>( string collection, object uid ) where T : class, new()
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, false );

		if ( relevantCollection is null )
			return null;

		relevantCollection.CachedDocuments.TryGetValue( uid, out var document );

		return document is null
			? null
			: ObjectPool.CloneObject( (T)document.Data, relevantCollection.DocumentClassType.FullName );
	}

	/// <summary>
	/// The same as SelectOne except faster since we can look it up by ID.
	/// </summary>
	public static T? SelectOneWithID<T>( object uid ) where T : class, new()
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
			return SelectOneWithID<T>( collectionAttr.Name, uid );

		Log.Error( $"Type {type.FullName} is not a collection" );
		return null;
	}

	/// <summary>
	/// Select all documents from the database where selector evaluates to true.
	/// </summary>
	public static List<T> Select<T>( string collection, Func<T, bool>? selector = null ) where T : class, new()
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, false );
		List<T> output = new();

		if ( relevantCollection is null )
			return output;

		foreach ( var pair in relevantCollection.CachedDocuments )
		{
			if ( selector is null )
			{
				output.Add( ObjectPool.CloneObject( (T)pair.Value.Data, relevantCollection.DocumentClassType.FullName ) );
			}
			else
			{
				if ( selector.Invoke( (T)pair.Value.Data ) )
					output.Add(
						ObjectPool.CloneObject( (T)pair.Value.Data, relevantCollection.DocumentClassType.FullName ) );
			}
		}

		return output;
	}

	/// <summary>
	/// Select all documents from the database where selector evaluates to true.
	/// </summary>
	public static List<T> Select<T>( Func<T, bool>? selector = null ) where T : class, new()
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
			return Select( collectionAttr.Name, selector );

		Log.Error( $"Type {type.FullName} is not a collection" );
		return new List<T>();
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
	public static List<T> SelectUnsafeReferences<T>( string collection, Func<T, bool> selector ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, false );
		List<T> output = new();

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
	public static List<T> SelectUnsafeReferences<T>( Func<T, bool> selector ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
			return SelectUnsafeReferences( collectionAttr.Name, selector );

		Log.Error( $"Type {type.FullName} is not a collection" );
		return new List<T>();
	}

	/// <summary>
	/// Delete all documents from the database where selector evaluates to true.
	/// </summary>
	public static void Delete<T>( string collection, Predicate<T> selector ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, false );
		if ( relevantCollection is null ) return;

		List<object> idsToDelete = new();

		foreach ( var pair in relevantCollection.CachedDocuments )
		{
			if ( selector.Invoke( (T)pair.Value.Data ) )
				idsToDelete.Add( pair.Key );
		}

		foreach ( var id in idsToDelete )
		{
			relevantCollection.CachedDocuments.TryRemove( id, out _ );

			var attempt = 0;
			var error = "";

			while ( true )
			{
				if ( attempt++ >= 10 )
					throw new RoverDatabaseException(
						$"failed to delete document from collection \"{collection}\" after 10 tries: " + error );

				error = FileController.DeleteDocument( collection, id );
				if ( string.IsNullOrEmpty( error ) ) break;
			}
		}
	}

	/// <summary>
	/// Delete all documents from the database where selector evaluates to true.
	/// </summary>
	public static void Delete<T>( Predicate<T> selector ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
		{
			Delete( collectionAttr.Name, selector );
			return;
		}

		Log.Error( $"Type {type.FullName} is not a collection" );
	}

	/// <summary>
	/// The same as Delete except faster since we can look it up by ID.
	/// </summary>
	public static void DeleteWithID<T>( string collection, object id ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, false );
		if ( relevantCollection is null ) return;

		relevantCollection.CachedDocuments.TryRemove( id, out _ );

		var attempt = 0;

		while ( true )
		{
			if ( attempt++ >= 10 )
				throw new RoverDatabaseException(
					$"failed to delete document from collection \"{collection}\" after 10 tries - is the file in use by something else?" );

			if ( FileController.DeleteDocument( collection, id ) == null )
				break;
		}
	}

	/// <summary>
	/// The same as Delete except faster since we can look it up by ID.
	/// </summary>
	public static void DeleteWithID<T>( object id ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
		{
			DeleteWithID<T>( collectionAttr.Name, id );
			return;
		}

		Log.Error( $"Type {type.FullName} is not a collection" );
	}

	/// <summary>
	/// Return whether there are any documents in the database where selector evaluates
	/// to true.
	/// </summary>
	public static bool Any<T>( string collection, Func<T, bool> selector ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, false );

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
	public static bool Any<T>( Func<T, bool> selector ) where T : class
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
			return Any( collectionAttr.Name, selector );

		Log.Error( $"Type {type.FullName} is not a collection" );
		return false;
	}

	/// <summary>
	/// The same as Any except faster since we can look it up by ID.
	/// </summary>
	public static bool AnyWithID<T>( string collection, object id )
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var relevantCollection = Cache.Cache.GetCollectionByName<T>( collection, false );
		return relevantCollection is not null && relevantCollection.CachedDocuments.ContainsKey( id );
	}

	/// <summary>
	/// The same as Any except faster since we can look it up by ID.
	/// </summary>
	public static bool AnyWithID<T>( object id )
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		var type = TypeLibrary.GetType<T>();
		var collectionAttr = type.GetAttribute<CollectionAttribute>();

		if ( collectionAttr is not null )
			return AnyWithID<T>( collectionAttr.Name, id );

		Log.Error( $"Type {type.FullName} is not a collection" );
		return false;
	}

	/// <summary>
	/// Deletes everything, forever.
	/// </summary>
	public static void DeleteAllData()
	{
		if ( !IsInitialised )
			InitializeAsync().GetAwaiter().GetResult();

		Cache.Cache.WipeStaticFields();

		var attempt = 0;
		string? error = null;

		while ( true )
		{
			if ( attempt++ >= 10 )
				throw new RoverDatabaseException( $"failed to load collections after 10 tries: {error}" );

			error = FileController.WipeFilesystem();
			if ( error is null ) return;
		}
	}

	/// <summary>
	/// Call this to gracefully shut-down the database. It is recommended to call this
	/// when your server is shutting down to make sure all recently-changed data is saved,
	/// if that's important to you. 
	/// <br/> <br/>
	/// Any operations ongoing at the time Shutdown is called are not guaranteed to be
	/// written to disk.
	/// </summary>
	public static void Shutdown()
	{
		RoverDB.Shutdown.ShutdownDatabase();
	}
}
