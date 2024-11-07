using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoverDB.Attributes;
using RoverDB.CodeGenerators;
using RoverDB.Exceptions;
using RoverDB.IO;
using RoverDB.Testing;
using Sandbox;

namespace RoverDB;

[TestClass]
public partial class RoverDatabaseTest
{
	[TestCleanup]
	public void Cleanup()
	{
		Config.OBFUSCATE_FILES = false;

		if ( !RoverDatabase.IsInitialised )
			RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.DeleteAllData();
		RoverDatabase.Shutdown();
	}

	[TestMethod]
	public void TextObfuscationWorks()
	{
		var text = Obfuscation.ObfuscateFileText( "Wow! I love bacon! 豚肉が美味しい！" );
		text = Obfuscation.UnobfuscateFileText( text );

		Assert.AreEqual( "Wow! I love bacon! 豚肉が美味しい！", text );
	}

	[TestMethod]
	public void TextObfuscationWorks_WithRealisticExample()
	{
		var original = @"{
  ""UID"": ""76561197997412036"",
  ""Health"": 100,
  ""BankBalance"": 0,
  ""Cash"": 10000,
  ""LastOnline"": ""2024-09-08T02:40:07.3391207\u002B01:00"",
  ""PlayerName"": ""anthonysharpy"",
  ""JumpCount"": 0,
  ""LivingThingsKilledCount"": 0,
  ""Achievements"": [],
  ""Hunger"": 100,
  ""EnduranceSkillLevel"": 0,
  ""EnduranceSkillLastDeterioratedTime"": 0,
  ""CompletedQuests"": [],
  ""StoredItems"": [
    {
      ""Rarity"": 1,
      ""BackgroundColour"": ""0.9071,0.79477,0.33716,0.3"",
      ""UniqueID"": ""a70f35fa-cbdf-420e-9523-efce79e9c4c1"",
      ""XPos"": -1,
      ""YPos"": -1,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""Fists"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    },
    {
      ""Rarity"": 1,
      ""BackgroundColour"": ""0.61983,0.48797,0.19491,0.3"",
      ""UniqueID"": ""51becde0-ab0d-4103-b0d0-689eecfc5e00"",
      ""XPos"": -1,
      ""YPos"": -1,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""Item Placer"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    },
    {
      ""Rarity"": 3,
      ""BackgroundColour"": ""0.15048,0.9476,0.72171,0.3"",
      ""UniqueID"": ""8a73055a-ae58-4759-9172-afeb5d73de61"",
      ""XPos"": 0,
      ""YPos"": 0,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""Grenade"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    },
    {
      ""Rarity"": 3,
      ""BackgroundColour"": ""0.78122,0.13411,0.26792,0.3"",
      ""UniqueID"": ""f859ae80-e14b-43f9-af88-3887635efc63"",
      ""XPos"": 1,
      ""YPos"": 0,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""Grenade"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    },
    {
      ""Rarity"": 3,
      ""BackgroundColour"": ""0.30573,0.77256,0.48727,0.3"",
      ""UniqueID"": ""cbe5b32d-7afb-4b09-98c5-0b1eec6688bb"",
      ""XPos"": 2,
      ""YPos"": 0,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""MP5"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    },
    {
      ""Rarity"": 3,
      ""BackgroundColour"": ""0.57136,0.81519,0.96548,0.3"",
      ""UniqueID"": ""dbe6f248-7eae-4e58-9ce4-800b6e73721b"",
      ""XPos"": 6,
      ""YPos"": 0,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""Grenade"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    },
    {
      ""Rarity"": 3,
      ""BackgroundColour"": ""0.17717,0.17827,0.75903,0.3"",
      ""UniqueID"": ""e6aa5962-c1bf-4326-b22e-1edee07f2d98"",
      ""XPos"": 0,
      ""YPos"": 1,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""Grenade"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    },
    {
      ""Rarity"": 3,
      ""BackgroundColour"": ""0.97016,0.52173,0.33395,0.3"",
      ""UniqueID"": ""2531c680-e2c5-49ad-aa88-b86446e518ea"",
      ""XPos"": 1,
      ""YPos"": 1,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""Ammo"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    },
    {
      ""Rarity"": 3,
      ""BackgroundColour"": ""0.87892,0.43858,0.34768,0.3"",
      ""UniqueID"": ""b2a04888-e0da-4d16-a877-89ca0e201e4d"",
      ""XPos"": 6,
      ""YPos"": 1,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""Ammo"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    },
    {
      ""Rarity"": 3,
      ""BackgroundColour"": ""0.04801,0.02349,0.17021,0.3"",
      ""UniqueID"": ""4f484a7c-d7b7-4df7-9557-cbce6188d3f9"",
      ""XPos"": 0,
      ""YPos"": 2,
      ""Durability"": 100,
      ""Price"": 0,
      ""ItemName"": ""Grenade"",
      ""PickupDisabled"": false,
      ""AmmoInClip"": 0
    }
  ],
  ""EquippedItemID"": ""30cf9992-1c8e-4330-b79c-090681fcbeeb"",
  ""CurrentlyEquippedSlot"": -1,
  ""CurrentlyEquippedSlotIndex"": 0,
  ""Stamina"": 100
}";

		var text = Obfuscation.ObfuscateFileText( original );
		text = Obfuscation.UnobfuscateFileText( text );

		Assert.AreEqual( original, text );
	}

	[TestMethod]
	public void SelectingUnitialisedCollectionReturnsEmptyList()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var results = RoverDatabase.Select<TestClasses.ReadmeExample>( "players", x => x.Health == 50 );

		Assert.AreEqual( 0, results.Count );
	}

	[TestMethod]
	public void NullUIDWorks()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var document = new TestClasses.NullUIDClass();

		RoverDatabase.Insert( "nulluidtest", document );

		Assert.AreEqual( 32, document.UID.Length );
	}

	[TestMethod]
	public void NoUIDThrowsException()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var document = new TestClasses.NoUIDClass();

		var e = Assert.ThrowsException<RoverDatabaseException>( () => RoverDatabase.Insert( "nouidtest", document ) );

		Assert.AreEqual( $"cannot handle a document without a \"UID\" property - make sure your data " +
			"class has a public property called UID, like this: \"[Saved] public string UID { get; set; }\"", e.Message );
	}

	[TestMethod]
	public void CantPutDifferentClassTypesInSameCollection()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var document1 = new TestClasses.ValidClass1();

		RoverDatabase.Insert( "test2", document1 );

		var document2 = new TestClasses.ValidClass2();

		var e = Assert.ThrowsException<RoverDatabaseException>( () => RoverDatabase.Insert( "test2", document2 ) );

		Assert.AreEqual( "there is no registered instance pool for the type TestClasses+ValidClass2 - " +
			"are you using the wrong class type for this collection?", e.Message );
	}

	[TestMethod]
	public void CopySavedData()
	{
		var document1 = new TestClasses.ClassWithNonSavedProperty();
		document1.Health = 50;
		document1.Name = "Steve";

		var document2 = new TestClasses.ClassWithNonSavedProperty();

		Assert.AreEqual( 0, document2.Health );
		Assert.AreEqual( null, document2.Name );

		RoverDatabase.CopySavedData<TestClasses.ClassWithNonSavedProperty>( document1, document2 );

		Assert.AreEqual( 50, document2.Health );
		Assert.AreEqual( null, document2.Name );
	}

	[TestMethod]
	public void CopySavedData_WorksForAutoSavedProperties()
	{
		var document1 = new TestClasses.AutoSavedReadmeExample();
		document1.Health = 50;
		document1.Name = "Steve";
		document1.UID = "blahblah";
		document1.Items = new List<string>{ "banana", "gun", "shoe" };

		var document2 = new TestClasses.AutoSavedReadmeExample();

		Assert.AreEqual( 0, document2.Health );
		Assert.AreEqual( null, document2.Name );
		Assert.AreEqual( null, document2.UID );
		Assert.AreEqual( 0, document2.Items.Count );

		RoverDatabase.CopySavedData<TestClasses.AutoSavedReadmeExample>( document1, document2 );

		Assert.AreEqual( 50, document2.Health );
		Assert.AreEqual( "Steve", document2.Name );
		Assert.AreEqual( "blahblah", document2.UID );
		Assert.AreEqual( 3, document2.Items.Count );
	}

	[TestMethod]
	public void CantGetDifferentClassTypesFromSameCollection()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var document1 = new TestClasses.ValidClass1();
		document1.Health = 50;

		RoverDatabase.Insert( "test3", document1 );

		var e = Assert.ThrowsException<InvalidCastException>(
			() => RoverDatabase.SelectOneWithID<TestClasses.ValidClass1Copy>( "test3", document1.UID ) );

		Assert.AreEqual( "Unable to cast object of type 'ValidClass1' to type 'ValidClass1Copy'.",
			e.Message );
	}

	[TestMethod]
	public void ReadmeExampleWorks()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var readmeExample = new TestClasses.ReadmeExample();

		Assert.AreEqual( null, readmeExample.UID );

		readmeExample.Health = 100;
		readmeExample.Name = "Bob";

		RoverDatabase.Insert( "players", readmeExample );

		Assert.AreEqual( 32, readmeExample.UID.Length );

		var playerWith100Health = RoverDatabase.SelectOne<TestClasses.ReadmeExample>( "players", 
			x => x.Health == 100 );

		Assert.AreEqual( "Bob", playerWith100Health.Name );

		RoverDatabase.DeleteWithID<TestClasses.ReadmeExample>( "players", playerWith100Health.UID );
	}

	[TestMethod]
	public void AutoSavedWorks()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var data = new TestClasses.AutoSavedReadmeExample();
		data.Health = 57;
		data.UID = "91246385";

		// Mock all this since codegen doesn't seem to work during tests.
		var property = new Sandbox.WrappedPropertySet<float>()
		{
			Setter = ( float value ) => data.Health = value,
			Value = 57,
			Object = data,
			PropertyName = "Health",
			Attributes = new Attribute[]
			{
				new AutoSavedAttribute("example")
			}
		};

		RoverDatabaseAutoSavedEventHandler.AutoSave( property );

		var fetchedData = RoverDatabase.Select<TestClasses.AutoSavedReadmeExample>( "example", x => x.Health == 57 );

		Assert.AreEqual( 1, fetchedData.Count );
		Assert.AreEqual( 8, fetchedData[0].UID.Length );
	}

	[TestMethod]
	public void SavingAndLoadingWorksWithObfuscation()
	{
		Config.OBFUSCATE_FILES = true;

		var readmeExample = new TestClasses.ReadmeExample();

		Assert.AreEqual( null, readmeExample.UID );

		readmeExample.Health = 100;
		readmeExample.Name = "Bob";

		RoverDatabase.Insert( "players", readmeExample );

		Assert.AreEqual( 32, readmeExample.UID.Length );

		var playerWith100Health = RoverDatabase.SelectOne<TestClasses.ReadmeExample>( "players",
			x => x.Health == 100 );

		Assert.AreEqual( "Bob", playerWith100Health.Name );

		RoverDatabase.DeleteWithID<TestClasses.ReadmeExample>( "players", playerWith100Health.UID );
	}

	[TestMethod]
	public void DatabaseWorksWithoutInitialisation()
	{
		var readmeExample = new TestClasses.ReadmeExample();

		Assert.AreEqual( null, readmeExample.UID );

		readmeExample.Health = 100;
		readmeExample.Name = "Bob";

		RoverDatabase.Insert( "players", readmeExample );

		Assert.AreEqual( 32, readmeExample.UID.Length );

		var playerWith100Health = RoverDatabase.SelectOne<TestClasses.ReadmeExample>( "players",
			x => x.Health == 100 );

		Assert.AreEqual( "Bob", playerWith100Health.Name );

		RoverDatabase.DeleteWithID<TestClasses.ReadmeExample>( "players", playerWith100Health.UID );
	}

	[TestMethod]
	public void SelectUnsafeReferences_DocumentDoesntChangeAfterNewInsert()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var document1 = new TestClasses.ValidClass1();
		document1.Health = 70;

		RoverDatabase.Insert( "players", document1 );

		var unsafeDocument = RoverDatabase.SelectUnsafeReferences<TestClasses.ValidClass1>("players",
			x => x.Health == 70 ).First();

		var safeDocument = RoverDatabase.SelectOne<TestClasses.ValidClass1>( "players",
			x => x.Health == 70 );

		safeDocument.Health = 80;

		RoverDatabase.Insert( "players", safeDocument );

		Assert.AreEqual( unsafeDocument.Health, 70 );
	}

	[TestMethod]
	public void TestInsertAndSelectOne()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", TestData.TestData1 );

		Assert.IsFalse( TestData.TestData1.UID.Length <= 10 );

		var data = RoverDatabase.SelectOne<TestClasses.ReadmeExample>( "players", x => x.UID == TestData.TestData1.UID );

		Assert.IsFalse( data.Name != TestData.TestData1.Name );
	}

	[TestMethod]
	public void TestInsertManyAndSelect()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var players = new List<TestClasses.ReadmeExample>{ TestData.TestData1, TestData.TestData2 };
		RoverDatabase.InsertMany<TestClasses.ReadmeExample>( "players", players );

		Assert.IsFalse( players[0].UID.Length <= 10 );
		Assert.IsFalse( players[1].UID.Length <= 10 );

		var data = RoverDatabase.Select<TestClasses.ReadmeExample>( "players", x => true );

		Assert.IsFalse( data[0].Name != TestData.TestData1.Name && data[0].Name != TestData.TestData2.Name );
		Assert.IsFalse( data[1].Name != TestData.TestData1.Name && data[1].Name != TestData.TestData2.Name );
	}

	[TestMethod]
	public void TestSelectOneWithID()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", TestData.TestData1 );

		Assert.IsFalse( TestData.TestData1.UID.Length <= 10 );

		var data = RoverDatabase.SelectOneWithID<TestClasses.ReadmeExample>( "players", TestData.TestData1.UID );

		Assert.IsFalse( data?.Name != TestData.TestData1.Name );
	}

	[TestMethod]
	public void TestDelete()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", TestData.TestData1 );

		RoverDatabase.Delete<TestClasses.ReadmeExample>( "players", x => x.UID == TestData.TestData1.UID );

		var data = RoverDatabase.SelectOneWithID<TestClasses.ReadmeExample>( "players", TestData.TestData1.UID );

		Assert.IsFalse( data != null );
	}

	/// <summary>
	/// Ensure that when we receive an object from the database, modifying it doesn't
	/// also modify the cache.
	/// </summary>
	[TestMethod]
	public void TestReferenceDoesntModifyCache()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", TestData.TestData1 );
		var data = RoverDatabase.SelectOneWithID<TestClasses.ReadmeExample>( "players", TestData.TestData1.UID );
		data.Level = 999;
		data = RoverDatabase.SelectOneWithID<TestClasses.ReadmeExample>( "players", TestData.TestData1.UID );

		Assert.IsFalse( data.Level == 999 );
	}

	[TestMethod]
	public void AutoSavedWorks_MultiThreaded()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var objects = new List<TestClasses.AutoSavedReadmeExample>();

		for (int i = 0; i < 10; i++ )
		{
			var data = new TestClasses.AutoSavedReadmeExample();
			data.Health = 57;
			data.UID = $"siffhdfdgf{i}";
			objects.Add( data );
		}

		var tasks = new List<Task>();

		// Make sure there's at least one auto save of each item.
		for ( int i = 0; i < 10; i++ )
		{
			var item = objects[i];

			tasks.Add( Task.Run( () =>
			{
				var property = new Sandbox.WrappedPropertySet<float>()
				{
					Setter = ( float value ) => item.Health = value,
					Value = 57,
					Object = item,
					PropertyName = "Health",
					Attributes = new Attribute[]
					{
					new AutoSavedAttribute("example")
					}
				};

				RoverDatabaseAutoSavedEventHandler.AutoSave( property );
			} ) );
		}

		for ( int i = 0; i < 1000; i++ )
		{
			var item = objects[Random.Shared.Int( 0, 9 )];

			tasks.Add( Task.Run( () =>
			{
				var property = new Sandbox.WrappedPropertySet<float>()
				{
					Setter = ( float value ) => item.Health = value,
					Value = 57,
					Object = item,
					PropertyName = "Health",
					Attributes = new Attribute[]
					{
						new AutoSavedAttribute("example")
					}
				};

				RoverDatabaseAutoSavedEventHandler.AutoSave( property );
			} ) );
		}

		Task.WaitAll( tasks.ToArray() );

		var fetchedData = RoverDatabase.Select<TestClasses.AutoSavedReadmeExample>( "example", x => x.Health == 57 );

		Assert.AreEqual( 10, fetchedData.Count );
	}

	/// <summary>
	/// Ensure that when we receive an object from the database, modifying it doesn't
	/// also modify the cache.
	/// </summary>
	[TestMethod]
	public void TestSpammingShutdownAndInitialise_DoesntCorruptData()
	{
		const int documents = 100;

		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", new TestClasses.ReadmeExample()
		{
			UID = "",
			Health = 100,
			Name = "TestPlayer1",
			Level = 10,
			LastPlayTime = DateTime.UtcNow,
			Items = new() { "gun", "frog", "banana" }
		} );

		// Warm the pool up to avoid console spam about not being able to load objects from the pool.
		Task.Delay( 4000 ).GetAwaiter().GetResult();

		var data = new List<TestClasses.ReadmeExample>();

		for ( int i = 0; i < documents - 1; i++ )
		{
			data.Add( new TestClasses.ReadmeExample()
			{
				UID = "",
				Health = 100,
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		RoverDatabase.InsertMany<TestClasses.ReadmeExample>( "players", data );

		Assert.AreEqual( documents, RoverDatabase.Select<TestClasses.ReadmeExample>( "players", x => x.Health == 100 ).Count() );

		var tasks = new List<Task>();

		for ( int i = 0; i < 100; i++ )
		{
			tasks.Add( GameTask.RunInThreadAsync( async () => await RoverDatabase.InitializeAsync() ) );
			tasks.Add( GameTask.RunInThreadAsync( () => RoverDatabase.Shutdown() ) );
		}

		Task.WaitAll( tasks.ToArray() );

		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		Assert.AreEqual( documents, RoverDatabase.Select<TestClasses.ReadmeExample>( "players", x => x.Health == 100 ).Count() );
	}

	[TestMethod]
	public void TestChangingTypeDefinition_DoesntLoseData()
	{
		Cache.Cache.DisableCacheWriting();
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		var document = new TestClasses.ReadmeExample()
		{
			UID = "",
			Health = 100,
			Name = "TestPlayer1",
			Level = 10,
			Items = new() { "gun", "frog", "banana" }
		};

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", document );

		var playerDocument = RoverDatabase.SelectOneWithID<TestClasses.ReadmeExample>( "players", document.UID );

		Assert.AreEqual( 100, playerDocument.Health );
		Assert.AreEqual( "TestPlayer1", playerDocument.Name );
		Assert.AreEqual( 10, playerDocument.Level );
		Assert.AreEqual( "gun", playerDocument.Items[0] );
		Assert.AreEqual( "frog", playerDocument.Items[1] );
		Assert.AreEqual( "banana", playerDocument.Items[2] );

		// Force write this so we have it saved to file.
		Cache.Cache.ForceFullWrite();

		// Let's wipe the cache to remove any trace of this class, this will stop the data being
		// written to disk when we shutdown the database in a moment.
		Cache.Cache.WipeCaches();

		// Overwrite with a document with different fields. We are checking that the other
		// fields are not lost.
		var smallerDocument = new TestClasses.ReadmeExampleWithFewerFields()
		{
			UID = document.UID,
			Health = 50,
		};

		var ovewriteDocument = 
			new Document( smallerDocument, typeof(TestClasses.ReadmeExampleWithFewerFields), false, "players" );

		// Now overwrite the data.
		FileController.SaveDocument( ovewriteDocument );

		// Shutdown in order to fully clear and re-fetch data and caches.
		RoverDatabase.Shutdown();
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		// Now load the data using the original type - all the other data should be there, other
		// than the changed health which was overwritten.
		var finalDocument = RoverDatabase.SelectOneWithID<TestClasses.ReadmeExample>( "players", document.UID );

		Assert.AreEqual( 50, finalDocument.Health );
		Assert.AreEqual( "TestPlayer1", finalDocument.Name );
		Assert.AreEqual( 10, finalDocument.Level );
		Assert.AreEqual( "gun", finalDocument.Items[0] );
		Assert.AreEqual( "frog", finalDocument.Items[1] );
		Assert.AreEqual( "banana", finalDocument.Items[2] );
	}

	[TestMethod]
	public void TestRestartingDatabase_DoesntCorruptData()
	{
		const int documents = 100;

		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", new TestClasses.ReadmeExample()
		{
			UID = "",
			Health = 100,
			Name = "TestPlayer1",
			Level = 10,
			LastPlayTime = DateTime.UtcNow,
			Items = new() { "gun", "frog", "banana" }
		} );

		// Warm the pool up to avoid console spam about not being able to load objects from the pool.
		Task.Delay( 4000 ).GetAwaiter().GetResult();

		var data = new List<TestClasses.ReadmeExample>();

		for ( int i = 0; i < documents - 1; i++ )
		{
			data.Add( new TestClasses.ReadmeExample()
			{
				UID = "",
				Health = 100,
				Name = "TestPlayer1",
				Level = 10,
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		RoverDatabase.InsertMany<TestClasses.ReadmeExample>( "players", data );

		Assert.AreEqual( documents, RoverDatabase.Select<TestClasses.ReadmeExample>( "players", x => x.Health == 100 ).Count() );

		RoverDatabase.Shutdown();
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		Assert.AreEqual( documents, RoverDatabase.Select<TestClasses.ReadmeExample>( "players", x => x.Health == 100 ).Count() );
	}

	[TestMethod]
	public void TestDeleteWithID()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", TestData.TestData1 );

		RoverDatabase.DeleteWithID<TestClasses.ReadmeExample>( "players", TestData.TestData1.UID );

		var data = RoverDatabase.SelectOneWithID<TestClasses.ReadmeExample>( "players", TestData.TestData1.UID );

		Assert.IsFalse( data != null );
	}

	[TestMethod]
	public void TestAny()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", TestData.TestData1 );

		Assert.IsFalse( !RoverDatabase.Any<TestClasses.ReadmeExample>( "players", x => x.Level == TestData.TestData1.Level ) );
	}

	[TestMethod]
	public void TestAnyWithID()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", TestData.TestData1 );

		Assert.IsFalse( !RoverDatabase.AnyWithID<TestClasses.ReadmeExample>( "players", TestData.TestData1.UID ) );
	}


	[TestMethod]
	public void TestShutdown()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		TestData.TestData1.UID = "";
		RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", TestData.TestData1 );

		var collection = Cache.Cache.GetCollectionByName<TestClasses.ReadmeExample>( "players", false );

		Assert.AreEqual( 1, Cache.Cache.GetDocumentsAwaitingWriteCount() );

		RoverDatabase.Shutdown();

		Assert.AreEqual( 0, Cache.Cache.GetDocumentsAwaitingWriteCount() );
	}

	[TestMethod]
	public void TestConcurrencySafety()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		List<Task> tasks = new();

		for ( int i = 0; i < 10; i++ )
		{
			tasks.Add( Task.Run( () =>
			{
				var data = new TestClasses.ReadmeExample()
				{
					UID = "",
					Health = Game.Random.Next( 101 ),
					Name = "TestPlayer1",
					Level = 10,
					LastPlayTime = DateTime.UtcNow,
					Items = new() { "gun", "frog", "banana" }
				};

				RoverDatabase.InsertMany<TestClasses.ReadmeExample>( "players", new TestClasses.ReadmeExample[] { data, data, data, data } );
				RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", data );
				var results = RoverDatabase.Select<TestClasses.ReadmeExample>( "players", x => x.Health > 90 );

				if ( results.Count > 0 )
				{
					RoverDatabase.SelectOneWithID<TestClasses.ReadmeExample>( "players", results[0].UID );
					RoverDatabase.DeleteWithID<TestClasses.ReadmeExample>( "players", results[0].UID );
				}

				RoverDatabase.Delete<TestClasses.ReadmeExample>( "players", x => x.Health <= 20 );
				RoverDatabase.Any<TestClasses.ReadmeExample>( "players", x => x.Name == "Tim" );
			} ) );
		}

		Task.WaitAll( tasks.ToArray() );
	}

	[TestMethod]
	public void TestConcurrencySafety2()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		List<Task> tasks = new();

		List<TestClasses.ReadmeExample> loadsOfData = new();

		for ( int i = 0; i < 10000; i++ )
		{
			loadsOfData.Add( new TestClasses.ReadmeExample()
			{
				UID = "",
				Health = 100,
				Name = "TestPlayer",
				Level = Game.Random.Next( 10 ),
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		for ( int i = 0; i < 10; i++ )
		{
			tasks.Add( Task.Run( () =>
			{
				for ( int j = 0; j < 10000; j++ )
				{
					var data = new TestClasses.ReadmeExample()
					{
						UID = "",
						Health = 100,
						Name = "TestPlayer",
						Level = 10,
						LastPlayTime = DateTime.UtcNow,
						Items = new() { "gun", "frog", "banana" }
					};

					RoverDatabase.Insert<TestClasses.ReadmeExample>( "players", data );
					RoverDatabase.Insert<TestClasses.ReadmeExample>( "players_two", data );
				}
			} ) );

			tasks.Add( Task.Run( () =>
			{
				RoverDatabase.InsertMany<TestClasses.ReadmeExample>( "players", loadsOfData );
				RoverDatabase.InsertMany<TestClasses.ReadmeExample>( "players_two", loadsOfData );
			} ) );

			tasks.Add( Task.Run( () =>
			{
				RoverDatabase.Select<TestClasses.ReadmeExample>( "players", x => x.Health == 100 );
				RoverDatabase.Select<TestClasses.ReadmeExample>( "players_two", x => x.Name == "TestPlayer" );
			} ) );

			tasks.Add( Task.Run( () =>
			{
				RoverDatabase.Delete<TestClasses.ReadmeExample>( "players", x => x.Level == 5 || x.Level == 2 );
				RoverDatabase.Delete<TestClasses.ReadmeExample>( "players_two", x => x.Level == 5 || x.Level == 2 );
			} ) );
		}

		Task.WaitAll( tasks.ToArray() );
	}

	[TestMethod]
	public void TestConcurrencySafety_SpamCollections()
	{
		RoverDatabase.InitializeAsync().GetAwaiter().GetResult();

		List<Task> tasks = new();

		List<TestClasses.ReadmeExample> loadsOfData = new();

		for ( int i = 0; i < 100; i++ )
		{
			loadsOfData.Add( new TestClasses.ReadmeExample()
			{
				UID = "",
				Health = 100,
				Name = "TestPlayer",
				Level = Game.Random.Next( 10 ),
				LastPlayTime = DateTime.UtcNow,
				Items = new() { "gun", "frog", "banana" }
			} );
		}

		for ( int x = 0; x < 10; x++ )
		{
			for ( int i = 0; i < 100; i++ )
			{
				string collection = "collection" + i;

				tasks.Add( Task.Run( () =>
				{
					for ( int i = 0; i < 100; i++ )
					{
						RoverDatabase.Insert<TestClasses.ReadmeExample>( collection, loadsOfData[i] );
					}
				} ) );

				tasks.Add( Task.Run( () =>
				{
					RoverDatabase.Delete<TestClasses.ReadmeExample>( collection, x => x.Level == 5 || x.Level == 2 );
				} ) );
			}
		}

		Task.WaitAll( tasks.ToArray() );
	}
}
