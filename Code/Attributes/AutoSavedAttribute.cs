using System;
using Sandbox;

namespace RoverDB.Attributes;

/// <summary>
/// Add this attribute to a property to allow it to be saved to file. When the property
/// is modified, the whole class will be saved to file. Nothing will happen if the UID is not set.
/// 
/// While this is the most convenient approach, as this will save the document every time data
/// is changed, this will generally perform worse than [Saved].
/// </summary>
// [AttributeUsage( AttributeTargets.Property )]
// [CodeGenerator( CodeGeneratorFlags.WrapPropertySet | CodeGeneratorFlags.Instance,
// 	"RoverDB.CodeGenerators.RoverDatabaseAutoSavedEventHandler.AutoSave" )]
// public class AutoSavedAttribute : Attribute
// {
// 	public string CollectionName { get; set; }
//
// 	public AutoSavedAttribute( string collectionName )
// 	{
// 		CollectionName = collectionName;
// 	}
// }
