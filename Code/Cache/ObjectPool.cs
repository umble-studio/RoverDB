﻿using System;
using System.Collections.Concurrent;
using RoverDB.Exceptions;
using RoverDB.Extensions;
using RoverDB.Helpers;
using Sandbox;
using Sandbox.Internal;

namespace RoverDB.Cache;

/// <summary>
/// Provides class instances so that we don't need to create instances on-the-fly,
/// which is a major performance bottleneck.
/// </summary>
internal class ObjectPool
{
	private readonly Cache _cache;
	private DateTime _timeLastCheckedPool;
	private readonly ConcurrentDictionary<string, PoolTypeDefinition> _objectPool = new();

	public ObjectPool( Cache cache )
	{
		_cache = cache;
	}

	public T CloneObject<T>( T theObject, string classTypeName )
	{
		var instance = GetInstance<T>( theObject.GetType() );
		_cache.CopyClassData( theObject, instance, classTypeName );
		return instance;
	}

	public object CloneObject( object theObject, Type objectType )
	{
		var instance = GetInstance( objectType );
		_cache.CopyClassData( theObject, instance, objectType.FullName! );
		return instance;
	}

	public object GetInstance( Type classType )
	{
		var targetType = classType;

		classType = classType.GetCollectionType();
		var classTypeName = classType.FullName!;

		if ( !_objectPool.TryGetValue(classTypeName, out var value) )
		{
			throw new RoverDatabaseException( $"there is no registered instance pool for the type {classTypeName} - " +
			                                  "are you using the wrong class type for this collection?" );
		}

		if ( value.TypePool.TryTake( out var instance ) )
			return instance;

		// If we couldn't get an instance, then we just have to create a new one.
		return GlobalGameNamespace.TypeLibrary.Create<object>( targetType );
	}

	public T GetInstance<T>( Type classType )
	{
		// var baseClassType = classType;
		var baseType = classType.GetCollectionType();

		if ( _objectPool[baseType.FullName!].TypePool.TryTake( out var instance ) )
			return (T)instance;

		// If we couldn't get an instance, then we just have to create a new one.
		// return new T();

		return (T)GlobalGameNamespace.TypeLibrary.Create( classType.Name, classType );
	}

	/// <summary>
	/// Tell the pool that we want to pool this class type.
	/// </summary>
	public void TryRegisterType( Type classType )
	{
		Log.Info("Register type: " + classType);
		classType = classType.GetCollectionType(); 
		Log.Info("Continued to register type: " + classType);
		var classTypeName = classType.FullName!;

		
		// Different collections might use the same type. So this is possible.
		if ( _objectPool.ContainsKey( classTypeName ) )
			return;
		

		_objectPool[classTypeName] = new PoolTypeDefinition( classType, new ConcurrentBag<object>() );
	}

	public void TryCheckPool()
	{
		var now = DateTime.UtcNow;

		if ( now.Subtract( _timeLastCheckedPool ).TotalMilliseconds <= 1000 )
			return;

		_timeLastCheckedPool = now;

		foreach ( var poolPair in _objectPool )
		{
			if ( Config.ClassInstancePoolSize - poolPair.Value.TypePool.Count >=
			     Config.ClassInstancePoolSize / 2 )
			{
				GameTask.RunInThreadAsync( () => ReplenishPoolType( poolPair.Key, poolPair.Value.ObjectType ) );
			}
		}
	}

	// TODO - Need to rewrite this shit completely
	// We can't replenish pool of type that use an abstract class as base class
	// We need to create an instance of the concrete type instead of the base type (E.g Police instead of Job)
	// Currently, replenish create the wrong instance type, so we need to fix that
	private void ReplenishPoolType( string classTypeName, Type classType )
	{
		var concurrentList = _objectPool[classTypeName].TypePool;
		var instancesToCreate = Config.ClassInstancePoolSize - concurrentList.Count;

		Log.Info( "Replenishing pool of type: " + string.Join( ", ", classTypeName, classType ) );
		// if ( classType.IsAbstract ) return; -> Shit

		for ( var i = 0; i < instancesToCreate; i++ )
		{
			concurrentList.Add( GlobalGameNamespace.TypeLibrary.Create<object>( classType ) );
		}
	}
}
