using System;
using System.Collections.Concurrent;
using RoverDB.Exceptions;
using RoverDB.Helpers;
using Sandbox;
using Sandbox.Internal;

namespace RoverDB.Cache;

/// <summary>
/// Provides class instances so that we don't need to create instances on-the-fly,
/// which is a major performance bottleneck.
/// </summary>
internal static class ObjectPool
{
	private static DateTime _timeLastCheckedPool;
	private static ConcurrentDictionary<string, PoolTypeDefinition> _objectPool = new();

	public static void WipeStaticFields()
	{
		_objectPool = new ConcurrentDictionary<string, PoolTypeDefinition>();
		_timeLastCheckedPool = DateTime.UtcNow.AddHours( -1 );
	}

	public static T CloneObject<T>( T theObject, string classTypeName )
	{
		Log.Info("Clone instance for " + string.Join(", ", theObject.GetType(), classTypeName));
		
		var instance = GetInstance<T>( theObject.GetType() );
		Cloning.CopyClassData( theObject, instance, classTypeName );
		return instance;
	}

	public static object CloneObject( object theObject, Type objectType )
	{
		var instance = GetInstance( objectType );
		Cloning.CopyClassData( theObject, instance, objectType.FullName! );
		return instance;
	}

	public static object GetInstance( Type classType )
	{
		var targetType = classType;
		
		classType = classType.GetCollectionType();
		var classTypeName = classType.FullName!;
		
		if ( !_objectPool.ContainsKey(classTypeName) )
		{
			throw new RoverDatabaseException( $"there is no registered instance pool for the type {classTypeName} - " +
				"are you using the wrong class type for this collection?" );
		}

		if ( _objectPool[classTypeName].TypePool.TryTake( out var instance ) )
			return instance;

		Log.Info("Create instance for " + targetType.FullName);
		
		// If we couldn't get an instance, then we just have to create a new one.
		return GlobalGameNamespace.TypeLibrary.Create<object>( targetType );
	}

	public static T GetInstance<T>(Type classType)
	{
		// var baseClassType = classType;
		var baseType = classType.GetCollectionType();
		
		Log.Info("GetInstance1: " + string.Join(", ", typeof(T), baseType, classType));
		
		if ( _objectPool[baseType.FullName!].TypePool.TryTake( out var instance ) )
			return (T)instance;

		// If we couldn't get an instance, then we just have to create a new one.
		// return new T();

		Log.Info("GetInstance2: " + string.Join(", ", typeof(T), classType));
		return (T)GlobalGameNamespace.TypeLibrary.Create( classType.Name, classType );
	}

	/// <summary>
	/// Tell the pool that we want to pool this class type.
	/// </summary>
	public static void TryRegisterType( Type classType )
	{
		classType = classType.GetCollectionType();
		var classTypeName = classType.FullName!;
		
		// Different collections might use the same type. So this is possible.
		if ( _objectPool.ContainsKey( classTypeName ) )
			return;

		_objectPool[classTypeName] = new PoolTypeDefinition
		{
			ObjectType = classType,
			TypePool = new ConcurrentBag<object>()
		};
	}

	public static void TryCheckPool()
	{
		var now = DateTime.UtcNow;

		if ( now.Subtract( _timeLastCheckedPool ).TotalMilliseconds <= 1000 )
			return;

		_timeLastCheckedPool = now;

		foreach (var poolPair in _objectPool)
		{
			if ( Config.CLASS_INSTANCE_POOL_SIZE - poolPair.Value.TypePool.Count >= Config.CLASS_INSTANCE_POOL_SIZE / 2)
			{
				GameTask.RunInThreadAsync( () => ReplenishPoolType( poolPair.Key, poolPair.Value.ObjectType ) );
			}
		}
	}

	private static void ReplenishPoolType(string classTypeName, Type classType )
	{
		var concurrentList = _objectPool[classTypeName].TypePool;
		var instancesToCreate = Config.CLASS_INSTANCE_POOL_SIZE - concurrentList.Count;

		for ( var i = 0; i < instancesToCreate; i++ )
		{
			concurrentList.Add(GlobalGameNamespace.TypeLibrary.Create<object>( classType ));
		}
	}
}

internal struct PoolTypeDefinition
{
	public Type ObjectType;
	public ConcurrentBag<object> TypePool;
}
