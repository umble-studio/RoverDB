using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RoverDB.Exceptions;
using RoverDB.Helpers;
using RoverDB.IO;
using RoverDB.Testing;
using Sandbox;

namespace RoverDB.Cache;

static internal class Cache
{
	/// <summary>
	/// Indicates that a full or partial write to disk is in progress.
	/// </summary>
	public static readonly object WriteInProgressLock = new();

	/// <summary>
	/// All the stale documents.
	/// </summary>
	public static ConcurrentBag<Document> StaleDocuments = new();

	private static readonly ConcurrentDictionary<string, Collection> _collections = new();
	private static float _timeSinceLastFullWrite = 0;
	private static readonly object _timeSinceLastFullWriteLock = new();
	private static int _staleDocumentsFoundAfterLastFullWrite;
	private static int _staleDocumentsWrittenSinceLastFullWrite;
	private static float _partialWriteInterval = 1f / Config.PARTIAL_WRITES_PER_SECOND;
	private static TimeSince _timeSinceLastPartialWrite = 0;
	private static readonly object _collectionCreationLock = new();
	private static bool _cacheWriteEnabled = true;

	public static int GetDocumentsAwaitingWriteCount()
	{
		return StaleDocuments.Count;
	}

	/// <summary>
	/// Used in the tests when we want to invalidate everything in the caches.
	/// 
	/// A bit crude and doesn't wipe everything.
	/// </summary>
	public static void WipeCaches()
	{
		if ( !TestHelpers.IsUnitTests )
			throw new Exception( "this can only be called during tests" );

		StaleDocuments = new ConcurrentBag<Document>();

		foreach ( var collection in _collections )
			collection.Value.CachedDocuments = new ConcurrentDictionary<object, Document>();
	}

	/// <summary>
	/// Used in the tests when we want to do writing to disk manually.
	/// </summary>
	public static void DisableCacheWriting()
	{
		if ( !TestHelpers.IsUnitTests )
			throw new Exception( "this can only be called during tests" );

		_cacheWriteEnabled = false;
	}

	public static void WipeStaticFields()
	{
		lock ( WriteInProgressLock )
		{
			_collections.Clear();
			_timeSinceLastFullWrite = 0;
			_staleDocumentsFoundAfterLastFullWrite = 0;
			_staleDocumentsWrittenSinceLastFullWrite = 0;
			StaleDocuments.Clear();
			_partialWriteInterval = 1f / Config.PARTIAL_WRITES_PER_SECOND;
			_timeSinceLastPartialWrite = 0;
		}
	}

	public static Collection? GetCollectionByName<T>( string name, bool createIfDoesntExist )
	{
		return GetCollectionByName( name, createIfDoesntExist, typeof(T) );
	}

	public static Collection? GetCollectionByName( string name, bool createIfDoesntExist, Type documentType )
	{
		if ( !_collections.ContainsKey( name ) )
		{
			if ( createIfDoesntExist )
			{
				Log.Info( $"creating new collection \"{name}\"" );
				CreateCollection( name, documentType );
			}
			else
			{
				return null;
			}
		}

		return _collections[name];
	}

	private static float GetTimeSinceLastFullWrite()
	{
		lock ( _timeSinceLastFullWriteLock )
		{
			return _timeSinceLastFullWrite;
		}
	}

	private static void ResetTimeSinceLastFullWrite()
	{
		lock ( _timeSinceLastFullWriteLock )
		{
			_timeSinceLastFullWrite = 0;
		}
	}

	public static void CreateCollection( string name, Type documentClassType )
	{
		// Only allow one thread to create a collection at once or this will
		// be madness.
		lock ( _collectionCreationLock )
		{
			if ( _collections.ContainsKey( name ) ) return;
			
			ObjectPool.TryRegisterType( documentClassType );

			documentClassType = CollectionAttributeHelper.GetCollectionType( documentClassType )!.TargetType;
			Log.Info("Document class type: " + documentClassType.FullName);
			
			var newCollection = new Collection()
			{
				CollectionName = name,
				DocumentClassType = documentClassType,
				DocumentClassTypeSerialized = documentClassType.FullName!
			};

			FileController.CreateCollectionLock( name );
			_collections[name] = newCollection;

			var attempt = 0;
			var error = "";

			while ( true )
			{
				if ( attempt++ >= 10 )
					throw new RoverDatabaseException(
						$"failed to save \"{name}\" collection definition after 10 tries - is the file in use by something else?: {error}" );

				error = FileController.SaveCollectionDefinition( newCollection );
				if ( string.IsNullOrEmpty(error) ) break;
			}
		}
	}

	public static void InsertDocumentsIntoCollection( string collection, List<Document> documents )
	{
		foreach ( var document in documents )
			_collections[collection].CachedDocuments[document.DocumentId] = document;
	}

	public static void Tick()
	{
		if ( Initialization.CurrentDatabaseState is not DatabaseState.Initialised || !_cacheWriteEnabled )
			return;

		GameTask.RunInThreadAsync( () =>
		{
			lock ( _timeSinceLastFullWriteLock )
			{
				_timeSinceLastFullWrite += Config.TICK_DELTA;
			}

			if ( GetTimeSinceLastFullWrite() >= Config.PERSIST_EVERY_N_SECONDS )
			{
				// Do this immediately otherwise when the server is stuttering it can spam
				// full writes.
				ResetTimeSinceLastFullWrite();

				lock ( WriteInProgressLock )
				{
					FullWrite();
				}
			}
			else if ( _timeSinceLastPartialWrite > _partialWriteInterval )
			{
				PartialWrite();
				_timeSinceLastPartialWrite = 0;
			}
		} );
	}

	/// <summary>
	/// Force the cache to perform a full-write of all stale entries.
	/// </summary>
	public static void ForceFullWrite()
	{
		lock ( WriteInProgressLock )
		{
			Log.Info( "beginning forced full-write..." );

			ReevaluateStaleDocuments();
			FullWrite();

			Log.Info( "finished forced full-write..." );
		}
	}

	/// <summary>
	/// Figure out how many documents we should write for our next partial write.
	/// </summary>
	private static int GetNumberOfDocumentsToWrite()
	{
		var progressToNextWrite = GetTimeSinceLastFullWrite() / Config.PERSIST_EVERY_N_SECONDS;
		var documentsWeShouldHaveWrittenByNow = (int)(_staleDocumentsFoundAfterLastFullWrite * progressToNextWrite);
		var numberToWrite = documentsWeShouldHaveWrittenByNow - _staleDocumentsWrittenSinceLastFullWrite;

		return numberToWrite <= 0 ? 0 : numberToWrite;
	}

	/// <summary>
	/// Write some (but probably not all) of the stale documents to disk. The longer
	/// it's been since our last partial write, the more documents we will write.
	/// </summary>
	private static void PartialWrite()
	{
		try
		{
			lock ( WriteInProgressLock )
			{
				var numberOfDocumentsToWrite = GetNumberOfDocumentsToWrite();

				if ( numberOfDocumentsToWrite > 0 )
				{
					Log.Info( "performing partial write..." );

					PersistStaleDocuments( numberOfDocumentsToWrite );
				}
			}
		}
		catch ( Exception e )
		{
			throw new RoverDatabaseException( "partial write failed: " + e.StackTrace );
		}
	}

	/// <summary>
	/// Perform a full-write to (maybe) guarantee we meet our write deadline target.
	/// Also, re-evaluate cache to determine what is now stale.
	/// </summary>
	private static void FullWrite()
	{
		try
		{
			// Log.Info( "performing full write..." );

			// Persist any remaining items first.
			PersistStaleDocuments();
			_staleDocumentsWrittenSinceLastFullWrite = 0;

			ReevaluateStaleDocuments();
		}
		catch ( Exception e )
		{
			throw new RoverDatabaseException( "full write failed: " + e.StackTrace );
		}
	}

	/// <summary>
	/// Persist some of the stale documents to disk. We generally don't want to persist
	/// them all at once, as this can cause lag spikes.
	/// </summary>
	private static void PersistStaleDocuments( int numberToWrite = int.MaxValue )
	{
		var remainingDocumentCount = _staleDocumentsFoundAfterLastFullWrite - _staleDocumentsWrittenSinceLastFullWrite;

		// Log.Info( $"remaining documents left to write: {remainingDocumentCount}" );

		if ( numberToWrite > remainingDocumentCount )
			numberToWrite = remainingDocumentCount;

		var realCount = StaleDocuments.Count;

		if ( numberToWrite > realCount )
			numberToWrite = realCount;

		// Log.Info( $"we are persisting {numberToWrite} documents to disk now" );

		_staleDocumentsWrittenSinceLastFullWrite += numberToWrite;

		var misses = 0;
		var failures = 0;

		for ( var i = 0; i < numberToWrite; i++ )
		{
			if ( !StaleDocuments.TryTake( out var document ) )
			{
				misses++;
				continue;
			}

			if ( !PersistDocumentToDisk( document ) )
				failures++;
		}

		if ( misses > 0 )
			Log.Info( $"missed {misses} times when persisting stale documents..." );

		_staleDocumentsWrittenSinceLastFullWrite -= (misses + failures);
	}

	/// <summary>
	/// Returns true on success, false otherwise.
	/// </summary>
	private static bool PersistDocumentToDisk( Document document )
	{
		var attempt = 0;
		var error = "";

		while ( true )
		{
			if ( attempt++ >= 3 )
			{
				Log.Error(
					$"failed to persist document \"{document.DocumentId}\" from collection \"{document.CollectionName}\" to disk after 3 tries: " +
					error );
				return false;
			}

			error = FileController.SaveDocument( document );

			if ( string.IsNullOrEmpty(error) )
				return true;
		}
	}

	/// <summary>
	/// Re-examine the cache and figure out what's stale and so what needs writing to
	/// disk.
	/// </summary>
	private static void ReevaluateStaleDocuments()
	{
		// Log.Info( "re-evaluating stale documents..." );

		_staleDocumentsFoundAfterLastFullWrite = StaleDocuments.Count;

		// Log.Info( $"found {_staleDocumentsFoundAfterLastFullWrite} stale documents" );
	}
}
