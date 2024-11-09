using System;
using System.Linq;
using RoverDB.Attributes;
using RoverDB.Exceptions;
using RoverDB.Helpers;
using RoverDB.IO;
using Sandbox.Internal;

namespace RoverDB;

internal sealed class Document
{
	public readonly Type DocumentType;
	public readonly string CollectionName;

	/// <summary>
	/// This is also stored embedded in the Data object, but we keep it
	/// here as an easily-accessible copy for convenience. We call it UID instead
	/// of ID because s&amp;box already has its own "Id" field on components.
	/// </summary>
	[Saved]
	public string DocumentId { get; private set; }

	/// <summary>
	/// We could save the data as a dictionary, which would stop us from having to
	/// clone a new object on document creation. However, this would stop us from
	/// easily doing lambdas against the document data, so it's not really worth it.
	/// </summary>
	[Saved]
	public object Data { get; private set; }

	public Document( object data, bool needsCloning, string collectionName )
	{
		Data = data;

		var documentType = GlobalGameNamespace.TypeLibrary.GetType( data.GetType() );

		// if ( !PropertyDescriptionsCache.DoesClassHaveUniqueIdProperty( documentType.FullName!, data ) )
		// 	throw new RoverDatabaseException(
		// 		"cannot handle a document without a property marked with a Id attribute - make sure your data class has a public property called UID, like this: \"[Saved] public string UID { get; set; }\"" );

		if ( !CollectionAttributeHelper.TryGetAttribute( documentType, out _, out _ ) )
			throw new RoverDatabaseException( $"Type {documentType.FullName} is not a collection" );

		// var id = (string)GlobalGameNamespace.TypeLibrary.GetPropertyValue( data, "UID" );
		//
		// if ( id is not null && id.Length > 0 )
		// {
		// 	UID = id;
		// }
		// else
		// {
		// 	UID = Guid.NewGuid().ToString().Replace( "-", "" );
		//
		// 	// We DO want to modify the UID of the passed-in reference.
		// 	GlobalGameNamespace.TypeLibrary.SetProperty( data, "UID", UID );
		// }

		var properties = GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( data );
		var propertyId = properties.FirstOrDefault( x => x.Attributes.Any( a => a is IdAttribute ) );

		if ( propertyId is null )
		{
			throw new RoverDatabaseException(
				"Cannot handle a document without a property marked with a Id attribute - make sure your data class has a public property called UID, like this: \"[Saved] public string UID { get; set; }\"" );
		}

		var id = propertyId.GetValue( data );

		if ( id is not null )
		{
			DocumentId = id?.ToString()!;
		}
		else
		{
			throw new RoverDatabaseException( "Document id cannot be null or empty" );
		}
		// else
		// {
		// 	// UID = Guid.NewGuid().ToString().Replace( "-", "" );
		//
		// 	DocumentId = Guid.NewGuid().ToString().Replace( "-", "" );
		// 	Log.Info( "Guid: " + DocumentId );
		//
		// 	GlobalGameNamespace.TypeLibrary.SetProperty( data, propertyId.Name, DocumentId );
		// }

		DocumentType = documentType.TargetType;
		CollectionName = collectionName;

		// We want to avoid modifying a passed-in reference, so we clone it.
		// But this is redundant in some cases, in which case we don't do it.
		// if ( needsCloning )
		// 	data = ObjectPool.CloneObject( data, documentType.TargetType );
		//
		// Data = data;
		// Cache.Cache.StaleDocuments.Add( this );
	}

	internal void Save( FileController fileController )
	{
		var documentType = GlobalGameNamespace.TypeLibrary.GetType( Data.GetType() );
		
		Data = fileController.Cache.Pool.CloneObject( Data, documentType.TargetType );
		fileController.Cache.StaleDocuments.Add( this );
	}
}
