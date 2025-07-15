using Xunit;

namespace Example.IntegrationTests;

// ссылка на фикстуру если хотим переиспользовать контейнер для всех тестов
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<PostgresContainerFixture> { }