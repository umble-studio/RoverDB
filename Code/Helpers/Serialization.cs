using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RoverDB.Serializers;

namespace RoverDB.Helpers;

internal static class SerializationHelper
{
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		ReadCommentHandling = JsonCommentHandling.Skip,
		WriteIndented = Config.INDENT_JSON,
		Converters = { new GenericSavedDataConverter() }
	};

	public static string SerializeClass<T>( T theClass )
	{
		return JsonSerializer.Serialize( theClass, _jsonOptions );
	}

	public static T? DeserializeClass<T>( string data )
	{
		return JsonSerializer.Deserialize<T>( data, _jsonOptions );
	}

	public static string SerializeJsonObject( JsonObject obj )
	{
		return obj.ToJsonString( _jsonOptions );
	}

	public static string SerializeClass( object theClass, Type classType )
	{
		return JsonSerializer.Serialize( theClass, classType, _jsonOptions );
	}

	public static object? DeserializeClass( string data, Type type )
	{
		return JsonSerializer.Deserialize( data, type, _jsonOptions );
	}
}
