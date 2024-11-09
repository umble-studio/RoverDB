using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RoverDB.Exceptions;
using RoverDB.Helpers;
using RoverDB.IO;
using RoverDB.Testing;
using Sandbox;

namespace RoverDB.Cache;

internal class Cache
{
	private readonly FileController _fileController;

	/// <summary>
	/// Indicates that a full or partial write to disk is in progress.
	/// </summary>
	public readonly object WriteInProgressLock = new();

	/// <summary>
	/// All the stale documents.
	/// </summary>
	public ConcurrentBag<Document> StaleDocuments = new();

	private readonly ConcurrentDictionary<string, Collection> _collections = new();
	private float _timeSinceLastFullWrite = 0;
	private readonly object _timeSinceLastFullWriteLock = new();
	private int _staleDocumentsFoundAfterLastFullWrite;
	private int _staleDocumentsWrittenSinceLastFullWrite;
	private float _partialWriteInterval = 1f / Config.PARTIAL_WRITES_PER_SECOND;
	private TimeSince _timeSinceLastPartialWrite = 0;
	private readonly object _collectionCreationLock = new();
	private bool _cacheWriteEnabled = true;

	public Cache( FileController fileController )
	{
		_fileController = fileController;
	}

	public int GetDocumentsAwaitingWriteCount()
	{
		return StaleDocuments.Count;
	}

	/// <summary>
	/// Used in the tests when we want to invalidate everything in the caches.
	/// 
	/// A bit crude and doesn't wipe everything.
	/// </summary>
	public void WipeCaches()
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
	public void DisableCacheWriting()
	{
		if ( !TestHelpers.IsUnitTests )
			throw new Exception( "this can only be called during tests" );

		_cacheWriteEnabled = false;
	}

	public void WipeFields()
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

	public Collection? GetCollectionByName<T>( string name, bool createIfDoesntExist )
	{
		return GetCollectionByName( name, createIfDoesntExist, typeof(T) );
	}

	public Collection? GetCollectionByName( string name, bool createIfDoesntExist, Type documentType )
	{
		if ( _collections.TryGetValue(name, out var collection) )
			return collection;

		if ( createIfDoesntExist )
		{
			Log.Info( $"creating new collection \"{name}\"" );
			
			if ( !CreateCollection( name, documentType ) )
				return null;
		}
		else
		{
			return null;
		}

		return _collections[name];
	}

	private float GetTimeSinceLastFullWrite()
	{
		lock ( _timeSinceLastFullWriteLock )
		{
			return _timeSinceLastFullWrite;
		}
	}

	private void ResetTimeSinceLastFullWrite()
	{
		lock ( _timeSinceLastFullWriteLock )
		{
			_timeSinceLastFullWrite = 0;
		}
	}

	public bool CreateCollection( string name, Type documentClassType )
	{
		// Only allow one thread to create a collection at once or this will
		// be madness.
		lock ( _collectionCreationLock )
		{
			if ( _collections.ContainsKey( name ) )
				return false;

			ObjectPool.TryRegisterType( documentClassType );

			documentClassType = documentClassType.GetCollectionType();

			var newCollection = new Collection()
			{
				CollectionName = name,
				DocumentClassType = documentClassType,
				DocumentClassTypeSerialized = documentClassType.FullName!
			};

			_fileController.CreateCollectionLock( name );
			_collections[name] = newCollection;

			return _fileController.SaveCollectionDefinition( newCollection );
		}
	}

	public void InsertDocumentsIntoCollection( string collection, List<Document> documents )
	{
		foreach ( var document in documents )
			_collections[collection].CachedDocuments[document.DocumentId] = document;
	}

	public void Tick()
	{
		if ( RoverDatabase.Instance.State is not DatabaseState.Initialised || !_cacheWriteEnabled )
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
	public void ForceFullWrite()
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
	private int GetNumberOfDocumentsToWrite()
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
	private void PartialWrite()
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
	private void FullWrite()
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
	private void PersistStaleDocuments( int numberToWrite = int.MaxValue )
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
	private bool PersistDocumentToDisk( Document document )
	{
		var saved = _fileController.SaveDocument( document );

		if ( saved )
			return true;

		Log.Error(
			$"failed to persist document \"{document.DocumentId}\" from collection \"{document.CollectionName}\" to disk" );

		return false;
	}

	/// <summary>
	/// Re-examine the cache and figure out what's stale and so what needs writing to
	/// disk.
	/// </summary>
	private void ReevaluateStaleDocuments()
	{
		// Log.Info( "re-evaluating stale documents..." );

		_staleDocumentsFoundAfterLastFullWrite = StaleDocuments.Count;

		// Log.Info( $"found {_staleDocumentsFoundAfterLastFullWrite} stale documents" );
	}
}
