using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RoverDB.Exceptions;

namespace RoverDB.IO;

internal sealed class MockFileIOProvider : IFileIOProvider
{
	private readonly MockDirectory _fileSystem = new();

	public string ReadAllText( string file )
	{
		var data = _fileSystem.GetFile( file )?.Contents;

		Log.Info( $"reading text from file {file} (read {data?.Length ?? 0} characters)" );
		return data;
	}

	public void WriteAllText( string file, string text )
	{
		Log.Info( $"writing to file {file}" );

		_fileSystem.CreateFileAt( file, text );
	}

	public void CreateDirectory( string directory )
	{
		Log.Info( $"creating directory {directory}" );

		_fileSystem.CreateDirectoryAt( directory );
	}

	/// <summary>
	/// Don't know how a directory deletion could ever not be recursive but that's
	/// how s&amp;box has it.
	/// </summary>
	public void DeleteDirectory( string directory, bool recursive = false )
	{
		Log.Info( $"deleting directory {directory}" );

		_fileSystem.DeleteDirectoryAt( directory );
	}

	public bool DirectoryExists( string directory )
	{
		var result = _fileSystem.GetDirectory( directory ) is not null;

		Log.Info( $"checking if directory {directory} exists ({(result ? "it does" : "it doesn't")})" );

		return result;
	}

	public IEnumerable<string> FindFile( string folder, string pattern = "*", bool recursive = false )
	{
		if ( recursive )
			throw new RoverDatabaseException( "not supported" );
		if ( pattern is not "*" )
			throw new RoverDatabaseException( "not supported" );

		var result = _fileSystem.GetFilesInDirectory( folder )
			.Where( x => x.FileType is MockFileType.File )
			.ToList();

		Log.Info( $"finding files in directory {folder} (found {result.Count})" );

		return result.Select( x => x.Name );
	}

	public IEnumerable<string> FindDirectory( string folder, string pattern = "*", bool recursive = false )
	{
		if ( recursive )
			throw new RoverDatabaseException( "not supported" );
		if ( pattern is not "*" )
			throw new RoverDatabaseException( "not supported" );

		var result = _fileSystem.GetFilesInDirectory( folder )
			.Where( x => x.FileType is MockFileType.Directory )
			.ToList();

		Log.Info( $"finding directories in directory {folder} (found {result.Count})" );

		return result.Select( x => x.Name );
	}

	public void DeleteFile( string file )
	{
		Log.Info( $"deleting file {file}" );

		_fileSystem.DeleteFileAt( file );
	}

	private class MockFileBase
	{
		public string Name;
		public MockFileType FileType;
	}

	private class MockFile : MockFileBase
	{
		public string Contents;
	}

	private class MockDirectory : MockFileBase
	{
		private readonly ConcurrentDictionary<string, MockFileBase> Files = new();

		public void DeleteFileAt( string path )
		{
			var directoryPath = path.Split( '/' ).Where( x => x.Any() ).ToList();

			var fileName = directoryPath[^1];
			directoryPath.RemoveAt( directoryPath.Count - 1 );

			var directory = GetDirectory( string.Join( '/', directoryPath ) );
			if ( directory is null ) return;

			if ( !directory.Files.TryGetValue(fileName, out var file) ) return;
			if ( file.FileType is not MockFileType.File ) return;

			directory.Files.Remove( fileName, out _ );
		}

		public void DeleteDirectoryAt( string path )
		{
			var directoryPath = path.Split( '/' ).Where( x => x.Any() ).ToList();

			var directoryName = directoryPath[^1];
			directoryPath.RemoveAt( directoryPath.Count - 1 );

			var directory = GetDirectory( string.Join( '/', directoryPath ) );
			if ( directory is null ) return;

			if ( !directory.Files.TryGetValue(directoryName, out var directoryToDelete) ) return;
			if ( directoryToDelete.FileType is not MockFileType.Directory ) return;

			directory.Files.Remove( directoryName, out _ );
		}

		public void CreateDirectoryAt( string path )
		{
			if ( GetDirectory( path ) is not null ) return;

			var parts = path.Split( '/' ).Where( x => x.Any() ).ToList();
			var parentPath = "";

			// If there is only one part then we just create the directory on ourselves.
			if ( parts.Count is 1 )
			{
				Files[parts[0]] = new MockDirectory() { FileType = MockFileType.Directory, Name = parts[0], };
				return;
			}

			var folderName = parts[^1];

			// parts now only contains the parent directories.
			parts.RemoveAt( parts.Count - 1 );

			// Make sure parent paths exist.
			// Don't do the last path because that isn't a parent path.
			foreach ( var part in parts )
			{
				parentPath += part + "/";

				if ( GetDirectory( parentPath ) is null )
					CreateDirectoryAt( parentPath );
			}

			var directory = GetDirectory( parentPath );
			directory.Files[folderName] = new MockDirectory() { FileType = MockFileType.Directory, Name = folderName, };
		}

		public void CreateFileAt( string path, string contents )
		{
			DeleteFileAt( path );

			var parts = path.Split( '/' ).Where( x => x.Any() ).ToList();

			var fileName = parts[^1];
			parts.RemoveAt( parts.Count - 1 );

			var containingFolder = string.Join( '/', parts );

			var directory = GetDirectory( containingFolder );

			if ( directory is null )
			{
				CreateDirectoryAt( containingFolder );
				directory = GetDirectory( containingFolder );
			}

			directory.Files[fileName] = new MockFile()
			{
				FileType = MockFileType.File, Name = fileName, Contents = contents
			};
		}

		public MockDirectory GetDirectory( string directory )
		{
			if ( !directory.Any() || directory is "/" )
				return this;

			var parts = directory.Split( '/' ).Where( x => x.Any() );
			var current = this;

			foreach ( var part in parts )
			{
				if ( !current.Files.TryGetValue(part, out var next) )
					return null;

				if ( next.FileType is not MockFileType.Directory )
					return null;

				current = (MockDirectory)next;
			}

			return current;
		}

		public MockFile GetFile( string path )
		{
			var parts = path.Split( '/' ).Where( x => x.Any() ).ToList();
			var fileName = parts.Last();

			parts.RemoveAt( parts.Count - 1 );

			var directory = GetDirectory( string.Join( "/", parts ) );

			if ( directory is null )
				return null;

			if ( !directory.Files.TryGetValue(fileName, out var file) )
				return null;

			if ( file.FileType is not MockFileType.File )
				return null;

			return (MockFile)file;
		}

		public List<MockFileBase> GetFilesInDirectory( string directory )
		{
			var dir = GetDirectory( directory );

			return dir is not null
				? dir.Files.Select( x => x.Value ).ToList()
				: new List<MockFileBase>();
		}
	}

	private enum MockFileType
	{
		File,
		Directory
	}
}
