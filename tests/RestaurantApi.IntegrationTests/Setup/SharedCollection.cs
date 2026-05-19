namespace RestaurantApi.IntegrationTests.Setup;

[CollectionDefinition("DatabaseCollection")]
public class SharedCollection: ICollectionFixture<TestDatabaseFixture>
{
    
}