using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RoverDB.Helpers;
using Sandbox;

namespace RoverDB.IO;

internal partial class FileController
{
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

				foreach ( var fileName in files )
				{
					var contents =
						_provider.ReadAllText( $"{Config.DatabaseName}/{collection.CollectionName}/{fileName}" );

					try
					{
						Log.Info( "Deserializing " + string.Join( ", ", fileName, collection.DocumentClassType ) );

						var document = new Document(
							SerializationHelper.Deserialize( contents, collection.DocumentClassType ),
							collection.CollectionName );

						var fileNameGuid = Guid.Parse( fileName );
						
						if ( fileNameGuid != document.DocumentId )
						{
							Log.Error(
								$"failed loading document \"{fileName}\": the filename does not match the UID ({fileName} vs {document.DocumentId}) - see RepairGuide.txt" );
							return output;
						}

						output.Add( document );
					}
					catch ( Exception e )
					{
						Log.Error( $"failed loading document \"{fileName}\" - your JSON is probably invalid: " +
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
	public void DeleteDocument( string collection, object documentID )
	{
		try
		{
			lock ( _collectionWriteLocks[collection] )
				_provider.DeleteFile( $"{Config.DatabaseName}/{collection}/{documentID}" );
		}
		catch ( Exception e )
		{
			Log.Info( "failed to delete document: " + e.Message );
		}
	}
}
