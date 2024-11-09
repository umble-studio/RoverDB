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
	public Guid DocumentId { get; private set; }

	/// <summary>
	/// We could save the data as a dictionary, which would stop us from having to
	/// clone a new object on document creation. However, this would stop us from
	/// easily doing lambdas against the document data, so it's not really worth it.
	/// </summary>
	[Saved]
	public object Data { get; private set; }

	public Document( object data, string collectionName )
	{
		Data = data;

		var documentType = data.GetType();

		if ( !CollectionAttributeHelper.TryGetAttribute( documentType, out _ ) )
			throw new RoverDatabaseException( $"Type {documentType.FullName} is not a collection" );

		if ( !PropertyHelper.HasPropertyId( data ) )
		{
			Log.Error(
				"cannot handle a document without a property marked with a Id attribute" );
		}

		var propertyId = PropertyHelper.GetPropertyId( data );

		if ( !propertyId.IsPropertyGuid() )
		{
			Log.Error( "The Id property must be of type Guid" );
			return;
		}

		var propertyValue = propertyId.GetValue( data );

		if ( !Guid.TryParse( propertyValue?.ToString(), out var guid ) )
		{
			Log.Error("failed to parse document id. (wrong format)");
			return;
		}
		
		if ( guid == Guid.Empty )
		{
			DocumentId = Guid.NewGuid();
			propertyId.SetValue( Data, DocumentId );
		}
		else
		{
			DocumentId = guid;
		}
		
		DocumentType = documentType;
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
