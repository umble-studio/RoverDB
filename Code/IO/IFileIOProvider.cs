using System.Collections.Generic;

namespace RoverDB.IO;

/// <summary>
/// Defines an implementation of a class that provides file access.
/// </summary>
internal interface IFileIOProvider
{
	bool DirectoryExists( string directory );
	void CreateDirectory( string directory );
	void DeleteDirectory( string directory, bool recursive = false );
	void WriteAllText( string file, string text );
	string ReadAllText( string file );
	void DeleteFile( string file );
	IEnumerable<string> FindFile( string folder, string pattern = "*", bool recursive = false );
	IEnumerable<string> FindDirectory( string folder, string pattern = "*", bool recursive = false );
}
