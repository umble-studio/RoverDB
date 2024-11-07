using System.Runtime.CompilerServices;
using Sandbox;

[assembly: InternalsVisibleTo( "roverdb.unittest" )]

namespace RoverDB.Testing;

internal static class TestHelpers
{
	public static bool IsUnitTests => FileSystem.Data == null;
}
