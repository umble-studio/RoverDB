using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace RoverDB.Server;

public static class SBServer
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

	/// <summary>
	/// Calls an endpoint that returns data. The response is serialised as the given type T.
	/// <br/><br/>
	/// dataObject is an optional data class for the request that will be serialised into JSON.
	/// </summary>
	public static async Task<T?> CallEndpoint<T>( string endpointName, object? dataObject = null ) where T : class
	{
		var json = dataObject is not null ? JsonSerializer.Serialize( dataObject ) : null;
		if ( string.IsNullOrEmpty( json ) ) return null;

		var requestContent = BuildRequestContent( endpointName, json );
		var response = await SendRequest( requestContent );

		HandleResponseType( endpointName, response );
		return await ProcessDataResponse<T>( response, endpointName );
	}

	/// <summary>
	/// Calls an endpoint that does not return data.
	/// <br/><br/>
	/// dataObject is an optional data class for the request that will be serialised into JSON.
	/// </summary>
	public static async Task CallEndpoint( string endpointName, object? dataObject = null )
	{
		var json = dataObject is not null ? JsonSerializer.Serialize( dataObject ) : null;
		if ( string.IsNullOrEmpty( json ) ) return;

		var requestContent = BuildRequestContent( endpointName, json );
		var response = await SendRequest( requestContent );

		HandleResponseType( endpointName, response );
	}

	private static async Task<HttpResponseMessage> SendRequest( StringContent requestContent )
	{
		return await Http.RequestAsync( "https://roverdb.com/endpoint", "POST", requestContent );
	}

	private static StringContent BuildRequestContent( string endpointName, string? jsonData )
	{
		jsonData ??= "null";

		var message = "{" +
		              "\"userID\":\"" + Config.SBSERVER_USER_ID + "\"," +
		              "\"publicKey\":\"" + Config.SBSERVER_PUBLIC_KEY + "\"," +
		              "\"endpoint\":\"" + endpointName + "\"," +
		              "\"data\":" + jsonData + "" +
		              "}";

		return new StringContent( message, Encoding.UTF8, "application/json" );
	}

	private static async Task<T?> ProcessDataResponse<T>( HttpResponseMessage response, string endpointName )
		where T : class
	{
		if ( !response.IsSuccessStatusCode )
			return null;

		var responseData = await response.Content.ReadAsStringAsync();

		try
		{
			return JsonSerializer.Deserialize<T>( responseData, _jsonOptions );
		}
		catch ( Exception e )
		{
			throw new Exception( $"RoverDatabase Server: failed deserializing JSON response from server for endpoint " +
			                     $"{endpointName} - either your response type is wrong or there is a bug on the server: {e.Message} " +
			                     $"... {e.InnerException}" );
		}
	}

	private static void HandleResponseType( string endpointName, HttpResponseMessage response )
	{
		if ( Config.ON_ENDPOINT_ERROR_BEHAVIOUR is OnEndpointErrorBehaviour.DoNothing ) return;
		if ( response.IsSuccessStatusCode ) return;

		switch ( response.StatusCode )
		{
			case System.Net.HttpStatusCode.TooManyRequests:
				Log.Warning( $"failed calling endpoint {endpointName} - you have reached your rate limit" );
				break;
			case System.Net.HttpStatusCode.InternalServerError:
				Log.Warning( $"failed calling endpoint {endpointName} - internal server error (this is a bug)" );
				break;
			case System.Net.HttpStatusCode.Forbidden:
				Log.Warning( $"failed calling endpoint {endpointName} - forbidden (are your credentials correct?)" );
				break;
			case System.Net.HttpStatusCode.BadRequest:
				Log.Warning(
					$"failed calling endpoint {endpointName} - bad request (is your endpoint/request correct?)" );
				break;
			default:
				Log.Warning(
					$"failed calling endpoint {endpointName} - there was an unknown error (response code {response.StatusCode})" );
				break;
		}
	}
}
