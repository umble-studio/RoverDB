using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoverDB.Attributes;
using Sandbox.Internal;

namespace RoverDB.Serializers;

public class GenericSavedDataConverter : JsonConverter<object>
{
	public override object Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
	{
		var instance = GlobalGameNamespace.TypeLibrary.Create<object>( typeToConvert );

		var properties = GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( instance, true )
			.Where( prop => prop.Attributes.Any( a => a is SavedAttribute or AutoSavedAttribute ) )
			.ToList();

		if ( reader.TokenType is not JsonTokenType.StartObject )
			throw new JsonException();

		while ( reader.Read() )
		{
			if ( reader.TokenType is JsonTokenType.EndObject )
				return instance;

			if ( reader.TokenType is not JsonTokenType.PropertyName )
				throw new JsonException();

			var propertyName = reader.GetString();

			var property = properties.FirstOrDefault( prop =>
				string.Equals( prop.Name, propertyName, StringComparison.OrdinalIgnoreCase ) );

			if ( property is not null )
			{
				reader.Read();

				var value = JsonSerializer.Deserialize( ref reader, property.PropertyType );
				property.SetValue( instance, value );
			}
			else
			{
				reader.Skip();
			}
		}

		throw new JsonException( "Expected end of object." );
	}

	public override void Write( Utf8JsonWriter writer, object value, JsonSerializerOptions options )
	{
		writer.WriteStartObject();

		var properties = GlobalGameNamespace.TypeLibrary.GetPropertyDescriptions( value )
			.Where( prop => prop.Attributes.Any( a => a is SavedAttribute or AutoSavedAttribute ) );

		foreach ( var prop in properties )
		{
			writer.WritePropertyName( prop.Name );
			JsonSerializer.Serialize( writer, prop.GetValue( value ), prop.PropertyType );
		}

		writer.WriteEndObject();
	}

	/// <summary>
	/// Don't delete this as it does actually do something!
	/// </summary>
	public override bool CanConvert( Type typeToConvert )
	{
		// Optionally, you can refine this method to return false for types that shouldn't use this converter
		return true; // As a simple approach, return true to indicate it can convert any object
	}
}
