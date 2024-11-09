using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RoverDB.Cache;
using RoverDB.Helpers;
using RoverDB.Testing;
using Sandbox;
using Sandbox.Internal;

namespace RoverDB.IO;

internal class FileController
{
	/// <summary>
	/// Only let one thread write/read a collection at a time using this lock.
	/// </summary>
	private readonly Dictionary<string, object> _collectionWriteLocks = new();

	private IFileIOProvider _provider = null!;

	public Cache.Cache Cache { get; }

	public FileController()
	{
		Cache = new Cache.Cache( this );
	}

	public void Initialize()
	{
		if ( TestHelpers.IsUnitTests )
			_provider = new MockFileIOProvider();
		else
			_provider = new FileIOProvider();
	}

	public void CreateCollectionLock( string collection )
	{
		Log.Info( $"creating collection write lock for collection \"{collection}\"" );

		_collectionWriteLocks[collection] = new object();
	}

	/// <summary>
	/// Returns null on success, or the error message on failure.
	/// </summary>
	public bool DeleteDocument( string collection, object documentID )
	{
		try
		{
			lock ( _collectionWriteLocks[collection] )
				_provider.DeleteFile( $"{Config.DatabaseName}/{collection}/{documentID}" );

			return true;
		}
		catch ( Exception e )
		{
			Log.Info( "failed to delete document: " + e.Message );
			return false;
		}
	}

	/// <summary>
	/// Save the document to file. We use a JSON merge strategy, so that if the current file has
	/// data that this new document doesn't recognise, it is not lost (the JSON is merged).
	/// This stops data from being wiped when doing things like renaming fields.
	/// 
	/// Returns null on success, or the error message on failure.
	/// </summary>
	public bool SaveDocument( Document document )
	{
		try
		{
			string output;

			// Load document currently stored on disk, if there is one.
			var data = Config.MergeJson
				? _provider.ReadAllText( $"{Config.DatabaseName}/{document.CollectionName}/{document.DocumentId}" )
				: null;

			if ( Config.MergeJson && data is not null )
			{
				var currentDocument = JsonDocument.Parse( data );

				// Get data from the new document we want to save.
				var saveableProperties = Cache.GetPropertyDescriptionsForType(
					document.Data.GetType().ToString(), document.Data
				);

				var propertyValuesMap = new Dictionary<string, PropertyDescription>();

				foreach ( var property in saveableProperties )
					propertyValuesMap.Add( property.Name, property );

				// Construct a new JSON object.
				var jsonObject = new JsonObject();

				// Add data by iterating over fields of old version.
				foreach ( var oldDocumentProperty in currentDocument.RootElement.EnumerateObject() )
				{
					if ( propertyValuesMap.ContainsKey( oldDocumentProperty.Name ) )
					{
						// Prefer values from the newer document.
						var value = propertyValuesMap[oldDocumentProperty.Name].GetValue( document.Data );
						var type = propertyValuesMap[oldDocumentProperty.Name].PropertyType;

						jsonObject.Add( oldDocumentProperty.Name, JsonSerializer.SerializeToNode( value, type ) );
					}
					else
					{
						// If newer document doesn't have this field, use the value from old document.
						jsonObject.Add( oldDocumentProperty.Name,
							JsonNode.Parse( oldDocumentProperty.Value.GetRawText() ) );
					}
				}

				// Also add any new fields the old version might not have.
				foreach ( var property in propertyValuesMap )
				{
					if ( !jsonObject.ContainsKey( property.Key ) )
					{
						var value = propertyValuesMap[property.Key].GetValue( document.Data );
						var type = propertyValuesMap[property.Key].PropertyType;

						jsonObject.Add( property.Key, JsonSerializer.SerializeToNode( value, type ) );
					}
				}

				// Serialize the object we just created.
				output = SerializationHelper.SerializeJsonObject( jsonObject );
			}
			else
			{
				// If no file exists for this record then we can just serialise the class directly.
				output = SerializationHelper.Serialize( document.Data, document.DocumentType );
			}

			lock ( _collectionWriteLocks[document.CollectionName] )
			{
				_provider.WriteAllText( $"{Config.DatabaseName}/{document.CollectionName}/{document.DocumentId}",
					output );
			}

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to save document: " + e.Message );
			return false;
		}
	}

	/// <summary>
	/// The second return value is null on success, and contains the error message
	/// on failure.
	/// </summary>
	public List<string> ListCollectionNames()
	{
		try
		{
			return _provider.FindDirectory( Config.DatabaseName ).ToList();
		}
		catch ( Exception e )
		{
			Log.Error( "failed to list collection names: " + e.Message );
			return new List<string>();
		}
	}

	/// <summary>
	/// The second return value contains the error message (or null if successful).
	/// </summary>
	public Collection? LoadCollectionDefinition( string collectionName )
	{
		try
		{
			string data;

			if ( !_collectionWriteLocks.ContainsKey( collectionName ) )
				CreateCollectionLock( collectionName );

			lock ( _collectionWriteLocks[collectionName] )
			{
				data = _provider.ReadAllText( $"{Config.DatabaseName}/{collectionName}/definition.txt" );
			}

			if ( string.IsNullOrEmpty( data ) )
			{
				Log.Error( $"no definition.txt for collection \"{collectionName}\" found - see RepairGuide.txt" );
				return null;
			}

			Collection? collection;

			try
			{
				collection = SerializationHelper.Deserialize<Collection>( data );
			}
			catch ( Exception e )
			{
				Log.Error( $"error thrown when deserializing definition.txt for \"{collectionName}\": " +
				           e.StackTrace );
				return null;
			}

			if ( collection?.CollectionName != collectionName )
			{
				Log.Error(
					$"failed to load definition.txt for collection \"{collectionName}\" - the CollectionName in the definition.txt differed from the name of the directory ({collectionName} vs {collection?.CollectionName}) - see RepairGuide.txt" );
				return null;
			}

			try
			{
				collection.DocumentClassType = GlobalGameNamespace.TypeLibrary
					.GetType( collection.DocumentClassTypeSerialized )
					.TargetType;
			}
			catch ( Exception e )
			{
				Log.Error(
					$"couldn't load the type described by the definition.txt for collection \"{collectionName}\" - most probably you renamed or removed your data type - see RepairGuide.txt: " +
					e.StackTrace );
				return null;
			}

			return collection;
		}
		catch ( Exception e )
		{
			Log.Error( $"failed to load definition.txt for collection \"{collectionName}\": " +
			           e.StackTrace );
			return null;
		}
	}

	/// <summary>
	/// The second return value contains the error message (or null if successful).
	/// </summary>
	public List<Document> LoadAllCollectionsDocuments( Collection collection )
	{
		List<Document> output = new();

		try
		{
			lock ( _collectionWriteLocks[collection.CollectionName] )
			{
				var files = _provider.FindFile( $"{Config.DatabaseName}/{collection.CollectionName}/" )
					.Where( x => x is not "definition.txt" )
					.ToList();

				foreach ( var file in files )
				{
					var contents =
						_provider.ReadAllText( $"{Config.DatabaseName}/{collection.CollectionName}/{file}" );

					try
					{
						Log.Info( "Deserializing " + string.Join( ", ", file, collection.DocumentClassType ) );

						var document = new Document(
							SerializationHelper.Deserialize( contents, collection.DocumentClassType ),
							false,
							collection.CollectionName );

						if ( file != document.DocumentId )
						{
							Log.Error(
								$"failed loading document \"{file}\": the filename does not match the UID ({file} vs {document.DocumentId}) - see RepairGuide.txt" );
							return output;
						}

						output.Add( document );
					}
					catch ( Exception e )
					{
						Log.Error( $"failed loading document \"{file}\" - your JSON is probably invalid: " +
						           e.StackTrace );
						return output;
					}
				}
			}

			return output;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to load all collection documents: " + e.Message );
			return output;
		}
	}

	/// <summary>
	/// Returns null on success, or the error message on failure.
	/// </summary>
	public bool SaveCollectionDefinition( Collection collection )
	{
		try
		{
			var data = SerializationHelper.Serialize( collection );

			lock ( _collectionWriteLocks[collection.CollectionName] )
			{
				if ( !_provider.DirectoryExists( $"{Config.DatabaseName}/{collection.CollectionName}" ) )
					_provider.CreateDirectory( $"{Config.DatabaseName}/{collection.CollectionName}" );

				_provider.WriteAllText( $"{Config.DatabaseName}/{collection.CollectionName}/definition.txt", data );
			}

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to save collection definition: " + e.Message );
			return false;
		}
	}

	/// <summary>
	/// Returns null on success, or the error message on failure.
	/// </summary>
	private bool DeleteCollection( string name )
	{
		try
		{
			lock ( _collectionWriteLocks[name] )
			{
				_provider.DeleteDirectory( $"{Config.DatabaseName}/{name}", true );
			}

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to delete collection: " + e.Message );
			return false;
		}
	}

	/// <summary>
	/// Wipes all RoverDatabase files. Returns null on success and the error message on failure.
	/// </summary>
	public bool WipeFilesystem()
	{
		try
		{
			var collections = ListCollectionNames();

			if ( collections.Count is not 0 )
			{
				Log.Error( $"failed to wipe filesystem" );
				return false;
			}

			// Don't delete collection folders when we are half-way through writing to them.
			lock ( Cache.WriteInProgressLock )
			{
				foreach ( var collection in collections )
				{
					var delete = DeleteCollection( collection );
					if ( delete ) continue;

					Log.Error( $"failed to wipe filesystem" );
					return false;
				}
			}

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to wipe filesystem: " + e.Message );
			return false;
		}
	}

	/// <summary>
	/// Creates the directories needed for the database. Returns null on success, or the error message
	/// on failure.
	/// </summary>
	public bool EnsureFileSystemSetup()
	{
		try
		{
			if ( !_provider.DirectoryExists( Config.DatabaseName ) )
				_provider.CreateDirectory( Config.DatabaseName );

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to ensure filesystem setup: " + e.Message );
			return false;
		}
	}
}
