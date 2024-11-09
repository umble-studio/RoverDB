using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using RoverDB.Converters;

namespace RoverDB.Helpers;

internal static class SerializationHelper
{
	public static readonly JsonSerializerOptions _jsonOptions = new()
	{
		ReadCommentHandling = JsonCommentHandling.Skip,
		WriteIndented = false,
		Converters = { new GenericSavedDataConverter() }
	};

	public static string Serialize<T>( T value )
	{
		return Serialize( value!, typeof(T) );
	}

	public static string Serialize( object value, Type inputType )
	{
		return JsonSerializer.Serialize( value, inputType, _jsonOptions );
	}

	public static string SerializeJsonObject( JsonObject obj )
	{
		return obj.ToJsonString( _jsonOptions );
	}

	public static T? Deserialize<T>( string json )
	{
		return (T?)Deserialize( json, typeof(T) );
	}

	public static object? Deserialize( string json, Type type )
	{
		return JsonSerializer.Deserialize( json, type, _jsonOptions );
	}
}
