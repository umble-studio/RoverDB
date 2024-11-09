using System;
using System.Collections.Concurrent;
using RoverDB.Attributes;
using RoverDB.Exceptions;
using RoverDB.Helpers;
using RoverDB.IO;

namespace RoverDB;

internal sealed class Collection
{
	/// <summary>
	/// Due to s&amp;box restrictions we have to save a string of the class type.
	/// We'll convert it back to a type when we load the collection from file.
	/// </summary>
	[Saved]
	public string DocumentClassTypeSerialized { get; init; } = null!;

	[Saved] public string CollectionName { get; init; } = null!;

	public Type DocumentClassType = null!;

	/// <summary>
	/// All the documents in this collection.
	/// </summary>
	public ConcurrentDictionary<object, Document> Documents = new();

	/// <summary>
	/// This should be used to insert documents since this enforces that the class type is
	/// correct.
	/// </summary>
	public void InsertDocument( FileController fileController, Document document )
	{
		var documentType = document.Data.GetType().GetCollectionType();

		if ( documentType is null )
		{
			Log.Error( "failed to insert document because it doesn't have a collection attribute" );
			return;
		}

		if ( documentType.ToString() != DocumentClassTypeSerialized )
		{
			throw new RoverDatabaseException( $"cannot insert a document of type {document.Data.GetType().FullName} " +
			                                  $"into a collection which expects type {DocumentClassTypeSerialized}" );
		}

		Documents[document.DocumentId] = document;
		document.Save( fileController );
	}
}
