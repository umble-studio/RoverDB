using System;

namespace RoverDB.Attributes;

/// <summary>
/// Add this attribute to a property to allow it to be saved to file.
/// </summary>
[AttributeUsage( AttributeTargets.Property )]
public sealed class SavedAttribute : Attribute
{
}
