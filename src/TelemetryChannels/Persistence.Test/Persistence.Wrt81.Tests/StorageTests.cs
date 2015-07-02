namespace Microsoft.ApplicationInsights.PersistenceChannel.Wrt81.Tests
{
    using Microsoft.ApplicationInsights.PersistenceChannel.Tests;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

    /// <summary>
    /// Run storage tests.
    /// (No need to do a clean up (as in .Net Storage Tests) because once the unit tests finishes the app 
    /// is deleted and all the local storage is deleted with it.  
    /// <seealso cref="StorageTestsBase"/>.
    /// </summary>
    [TestClass]
    public class StorageTests : StorageTestsBase
    {     
    }
}
