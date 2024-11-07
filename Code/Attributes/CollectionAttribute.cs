using System;

namespace RoverDB.Attributes;

[AttributeUsage( AttributeTargets.Class )]
public sealed class CollectionAttribute : Attribute
{
	public string Name { get; }
	
	public CollectionAttribute( string name )
	{
		Name = name;
	}
}
