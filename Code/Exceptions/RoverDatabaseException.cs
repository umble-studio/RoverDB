using System;

namespace RoverDB.Exceptions;

public sealed class RoverDatabaseException : Exception
{
	public RoverDatabaseException( string message ) : base( message )
	{
	}
}
