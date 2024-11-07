using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RoverDB;

[TestClass]
public class TestInit
{
	[AssemblyInitialize]
	public static void ClassInitialize( TestContext context )
	{
		Sandbox.Application.InitUnitTest();
	}
}