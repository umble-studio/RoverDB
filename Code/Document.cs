using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RoverDB.Attributes;
using RoverDB.Exceptions;
using RoverDB.Extensions;
using RoverDB.Helpers;
using Sandbox;

namespace RoverDB;

internal sealed class Document
{
	private readonly object _writeLock = new();

	[Id, Saved, JsonPropertyName( "__id" )]
	public Guid DocumentId { get; set; }

	[Saved, JsonPropertyName( "__type" )] public string DocumentTypeSerialized { get; set; } = null!;
	[Saved, JsonPropertyName( "__data" )] public object Data { get; set; } = null!;

	/// <summary>
	/// Inject the collection name internally after the document as been created / loaded
	/// </summary>
	internal string CollectionName { get; set; } = null!;

	public void Initialize()
	{
		var documentType = Data.GetType();

		if ( !CollectionAttributeHelper.TryGetAttribute( documentType, out _ ) )
			throw new RoverDatabaseException( $"Type {documentType.FullName} is not a collection" );
		
		DocumentTypeSerialized = documentType.FullName!;

		if ( DocumentId != Guid.Empty ) return;
		DocumentId = Guid.NewGuid();
	}

	/// <summary>
	/// Returns null on success, or the error message on failure.
	/// </summary>
	public void Delete()
	{
		try
		{
			lock ( _writeLock )
				FileSystem.Data.DeleteFile( $"{Config.DatabaseName}/{CollectionName}/{DocumentId}" );
		}
		catch ( Exception e )
		{
			Log.Info( "failed to delete document: " + e.Message );
		}
	}

	public void SaveDocument()
	{
		try
		{
			string output;

			// Load document currently stored on disk, if there is one.
			var data = Config.MergeJson
				? FileSystem.Data.ReadAllText( $"{Config.DatabaseName}/{CollectionName}/{DocumentId}" )
				: null;

			if ( Config.MergeJson && data is not null )
			{
				var currentDocument = JsonDocument.Parse( data );

				// Get data from the new document we want to save.
				var saveableProperties = CachePropertyExtensions.GetPropertyDescriptionsForType(
					Data.GetType().ToString(), this
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
						var value = propertyValuesMap[oldDocumentProperty.Name].GetValue( this );
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
						var value = propertyValuesMap[property.Key].GetValue( this );
						var type = propertyValuesMap[property.Key].PropertyType;

						jsonObject.Add( property.Key, JsonSerializer.SerializeToNode( value, type ) );
					}
				}

				// Serialize the object we just created.
				output = SerializationHelper.SerializeJsonObject( jsonObject );
			}
			else
			{
				output = SerializationHelper.Serialize( this );
			}

			lock ( _writeLock )
			{
				FileSystem.Data.WriteAllText( $"{Config.DatabaseName}/{CollectionName}/{DocumentId}",
					output );
			}
		}
		catch ( Exception e )
		{
			Log.Error( "failed to save document: " + e.Message );
		}
	}
}
