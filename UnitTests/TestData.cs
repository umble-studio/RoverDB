using System;
using RoverDB.Testing;

namespace RoverDB;

internal static class TestData
{
	public static TestClasses.ReadmeExample TestData1 = new()
	{
		UID = "",
		Health = 100,
		Name = "TestPlayer1",
		Level = 10,
		LastPlayTime = DateTime.UtcNow,
		Items = new() { "gun", "frog", "banana" }
	};

	public static TestClasses.ReadmeExample TestData2 = new()
	{
		UID = "",
		Health = 90,
		Name = "TestPlayer2",
		Level = 15,
		LastPlayTime = DateTime.UtcNow,
		Items = new() { "apple", "box" }
	};
}
