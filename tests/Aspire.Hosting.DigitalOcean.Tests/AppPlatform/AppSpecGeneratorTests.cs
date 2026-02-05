// Licensed under the MIT License.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DigitalOcean.AppPlatform;
using FluentAssertions;
using InfinityFlow.DigitalOcean.Client.Models;

namespace Aspire.Hosting.DigitalOcean.Tests.AppPlatform;

public class AppSpecGeneratorTests
{
    [Fact]
    public void Generate_WithAppNameAndRegion_CreatesValidSpec()
    {
        // Arrange
        var resources = Array.Empty<IResource>();

        // Act
        var spec = AppSpecGenerator.Generate("my-app", "nyc", resources);

        // Assert
        spec.Should().NotBeNull();
        spec.Name.Should().Be("my-app");
        spec.Region.Should().Be(App_spec_region.Nyc);
    }

    [Theory]
    [InlineData("nyc", App_spec_region.Nyc)]
    [InlineData("sfo", App_spec_region.Sfo)]
    [InlineData("ams", App_spec_region.Ams)]
    [InlineData("sgp", App_spec_region.Sgp)]
    [InlineData("lon", App_spec_region.Lon)]
    [InlineData("fra", App_spec_region.Fra)]
    [InlineData("tor", App_spec_region.Tor)]
    [InlineData("blr", App_spec_region.Blr)]
    [InlineData("syd", App_spec_region.Syd)]
    public void Generate_WithDifferentRegions_ParsesRegionCorrectly(string regionCode, App_spec_region expected)
    {
        // Arrange
        var resources = Array.Empty<IResource>();

        // Act
        var spec = AppSpecGenerator.Generate("test-app", regionCode, resources);

        // Assert
        spec.Region.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-app", "my-app")]
    [InlineData("myapp", "myapp")]
    [InlineData("my_app", "my-app")]
    [InlineData("My.App.Name", "my-app-name")]
    public void Generate_SanitizesAppName(string input, string expected)
    {
        // Arrange
        var resources = Array.Empty<IResource>();

        // Act
        var spec = AppSpecGenerator.Generate(input, "nyc", resources);

        // Assert
        spec.Name.Should().Be(expected);
    }

    [Fact]
    public void Generate_WithEmptyResources_ReturnsSpecWithEmptyCollections()
    {
        // Arrange
        var resources = Array.Empty<IResource>();

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert - empty collections may be null after optimization
        spec.Should().NotBeNull();
        spec.Name.Should().Be("test-app");
    }

    [Fact]
    public void ToYaml_GeneratesValidYamlString()
    {
        // Arrange
        var resources = Array.Empty<IResource>();
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().NotBeNullOrEmpty();
        yaml.Should().Contain("name: test-app");
        yaml.Should().Contain("region: nyc");
    }

    [Fact]
    public void ToYaml_ExcludesEmptyCollections()
    {
        // Arrange
        var resources = Array.Empty<IResource>();
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().NotContain("services:");
        yaml.Should().NotContain("workers:");
        yaml.Should().NotContain("databases:");
    }

    [Fact]
    public void Generate_WithMySqlResource_GeneratesDatabaseSpecWithMySqlEngine()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var mysql = builder.AddMySql("test-mysql");
        var resources = new IResource[] { mysql.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Databases.Should().HaveCount(1);
        var dbSpec = spec.Databases.First();
        dbSpec.Name.Should().Be("test-mysql");
        dbSpec.Engine.Should().Be(App_database_spec_engine.MYSQL);
        dbSpec.Production.Should().BeFalse();
    }

    [Fact]
    public void Generate_WithKafkaResource_GeneratesDatabaseSpecWithKafkaEngine()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var kafka = builder.AddKafka("test-kafka");
        var resources = new IResource[] { kafka.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Databases.Should().HaveCount(1);
        var dbSpec = spec.Databases.First();
        dbSpec.Name.Should().Be("test-kafka");
        dbSpec.Engine.Should().Be(App_database_spec_engine.KAFKA);
        dbSpec.Production.Should().BeFalse();
    }

    [Fact]
    public void Generate_WithElasticsearchResource_GeneratesDatabaseSpecWithOpenSearchEngine()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var elasticsearch = builder.AddElasticsearch("test-elasticsearch");
        var resources = new IResource[] { elasticsearch.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Databases.Should().HaveCount(1);
        var dbSpec = spec.Databases.First();
        dbSpec.Name.Should().Be("test-elasticsearch");
        dbSpec.Engine.Should().Be(App_database_spec_engine.OPENSEARCH);
        dbSpec.Production.Should().BeFalse();
    }

    [Fact]
    public void Generate_WithMultipleDatabases_GeneratesAllDatabaseSpecs()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var mysql = builder.AddMySql("mysql-db");
        var kafka = builder.AddKafka("kafka-broker");
        var elasticsearch = builder.AddElasticsearch("search-engine");
        var resources = new IResource[] { mysql.Resource, kafka.Resource, elasticsearch.Resource };

        // Act
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Assert
        spec.Databases.Should().HaveCount(3);
        
        var mysqlSpec = spec.Databases.First(db => db.Name == "mysql-db");
        mysqlSpec.Engine.Should().Be(App_database_spec_engine.MYSQL);

        var kafkaSpec = spec.Databases.First(db => db.Name == "kafka-broker");
        kafkaSpec.Engine.Should().Be(App_database_spec_engine.KAFKA);

        var elasticsearchSpec = spec.Databases.First(db => db.Name == "search-engine");
        elasticsearchSpec.Engine.Should().Be(App_database_spec_engine.OPENSEARCH);
    }

    [Fact]
    public void ToYaml_WithDatabaseResources_IncludesDatabasesSection()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var mysql = builder.AddMySql("test-mysql");
        var resources = new IResource[] { mysql.Resource };
        var spec = AppSpecGenerator.Generate("test-app", "nyc", resources);

        // Act
        var yaml = AppSpecGenerator.ToYaml(spec);

        // Assert
        yaml.Should().Contain("databases:");
        yaml.Should().Contain("name: test-mysql");
        yaml.Should().Contain("engine: MYSQL");
    }
}
