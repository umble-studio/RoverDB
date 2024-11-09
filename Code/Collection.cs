using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoverDB.Attributes;
using RoverDB.Helpers;
using Sandbox;
using Sandbox.Internal;

namespace RoverDB;

internal sealed class Collection
{
	private readonly object _writeLocks = new();
	public readonly ConcurrentDictionary<Guid, Document> Documents = new();

	[Saved, JsonPropertyName("__name")] public string Name { get; init; } = null!;

	public Collection()
	{
	}

	public Collection( string name )
	{
		Name = name;
		SaveDefinition();
	}

	public bool Load()
	{
		var definition = LoadDefinition( Name );

		if ( definition is null )
		{
			Log.Error(
				$"found a folder for collection {Name} but the definition.txt was missing in that folder or failed to load" );
			return false;
		}

		LoadAllCollectionsDocuments();

		Log.Info( $"Loaded collection {Name} with {Documents.Count} documents" );
		return true;
	}

	public bool Save()
	{
		try
		{
			foreach ( var (_, document) in Documents )
				document.SaveDocument();

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to save collection: " + e.Message );
			return false;
		}
	}

	public bool Delete()
	{
		try
		{
			lock ( _writeLocks )
			{
				FileSystem.Data.DeleteDirectory( $"{Config.DatabaseName}/{Name}", true );
			}

			return true;
		}
		catch ( Exception e )
		{
			Log.Error( "failed to delete collection: " + e.Message );
			return false;
		}
	}

	private void LoadAllCollectionsDocuments()
	{
		try
		{
			lock ( _writeLocks )
			{
				var files = FileSystem.Data.FindFile( $"{Config.DatabaseName}/{Name}/" )
					.Where( x => x is not "definition.txt" )
					.ToList();

				foreach ( var fileName in files )
				{
					var contents =
						FileSystem.Data.ReadAllText( $"{Config.DatabaseName}/{Name}/{fileName}" );

					Log.Info( "Load file: " + contents );

					try
					{
						var document = SerializationHelper.Deserialize<Document>( contents );
						if ( document is null ) return;

						document.CollectionName = Name;

						// By default, the document data is a JsonElement. So we need to convert it to the correct type.
						var type = GlobalGameNamespace.TypeLibrary.GetType( document.DocumentTypeSerialized );
						document.Data = ((JsonElement)document.Data).Deserialize( type.TargetType )!;

						var fileNameGuid = Guid.Parse( fileName );

						if ( fileNameGuid != document.DocumentId )
						{
							Log.Error(
								$"failed loading document \"{fileName}\": the filename does not match the UID ({fileName} vs {document.DocumentId}) - see RepairGuide.txt" );
							return;
						}

						Documents[document.DocumentId] = document;
					}
					catch ( Exception e )
					{
						Log.Error( $"failed loading document \"{fileName}\" - your JSON is probably invalid: " +
						           e.Message );
					}
				}
			}
		}
		catch ( Exception e )
		{
			Log.Error( "failed to load all collection documents: " + e.Message );
		}
	}

	/// <summary>
	/// Returns null on success, or the error message on failure.
	/// </summary>
	private void SaveDefinition()
	{
		try
		{
			var data = SerializationHelper.Serialize( this );

			lock ( _writeLocks )
			{
				if ( !FileSystem.Data.DirectoryExists( $"{Config.DatabaseName}/{Name}" ) )
					FileSystem.Data.CreateDirectory( $"{Config.DatabaseName}/{Name}" );

				FileSystem.Data.WriteAllText( $"{Config.DatabaseName}/{Name}/definition.txt", data );
			}
		}
		catch ( Exception e )
		{
			Log.Error( "failed to save collection definition: " + e.Message );
		}
	}

	/// <summary>
	/// The second return value contains the error message (or null if successful).
	/// </summary>
	private Collection? LoadDefinition( string collectionName )
	{
		try
		{
			string data;

			lock ( _writeLocks )
			{
				data = FileSystem.Data.ReadAllText( $"{Config.DatabaseName}/{collectionName}/definition.txt" );
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

			if ( collection?.Name == collectionName )
				return collection;

			Log.Error(
				$"failed to load definition.txt for collection \"{collectionName}\" - the CollectionName in the definition.txt differed from the name of the directory ({collectionName} vs {collection?.Name}) - see RepairGuide.txt" );
			return null;
		}
		catch ( Exception e )
		{
			Log.Error( $"failed to load definition.txt for collection \"{collectionName}\": " +
			           e.StackTrace );
			return null;
		}
	}

	public void InsertDocument( Document document )
	{
		Documents[document.DocumentId] = document;
		document.SaveDocument();
	}

	public void DeleteDocument( Guid documentId )
	{
		var document = Documents[documentId];
		document.Delete();

		Documents.TryRemove( documentId, out _ );
	}

	/// <summary>
	/// The second return value is null on success, and contains the error message
	/// on failure.
	/// </summary>
	public static List<string> ListCollectionNames()
	{
		try
		{
			return FileSystem.Data.FindDirectory( Config.DatabaseName ).ToList();
		}
		catch ( Exception e )
		{
			Log.Error( "failed to list collection names: " + e.Message );
			return new List<string>();
		}
	}
}
