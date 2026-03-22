using Xunit;

namespace Score2Stream.Tests.MenuModuleTests.Base
{
    [CollectionDefinition("HeadlessUI")]
    public class HeadlessUICollection
        : ICollectionFixture<HeadlessSessionFixture>
    { }
}