#pragma warning disable
#pragma warning disable CS8604
#pragma warning disable CS8604
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;
using VAH.Backend.Models;
using VAH.Backend.Models.Events;
using VAH.Backend.Tests.Builders;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using VAH.Backend.Data.Interceptors;
using System.Linq;
using System.Threading.Tasks;

namespace VAH.Backend.Tests.Unit;

public class OutboxMessageTests
{
    [Fact]
    public async Task SaveChanges_WithDomainEvents_ShouldCreateOutboxMessages()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<InsertOutboxMessagesInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase("OutboxTestDb")
                   .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>());
        });

        var serviceProvider = services.BuildServiceProvider();
        using var context = serviceProvider.GetRequiredService<AppDbContext>();
        
        var collection = new CollectionBuilder().WithUserId("test-user").Build();
        context.Collections.Add(collection);
        await context.SaveChangesAsync();

        var asset = new AssetBuilder()
            .WithFileName("outbox-test.jpg")
            .WithCollectionId(collection.Id)
            .WithUserId("test-user")
            .Build();

        // Act
        asset.AddDomainEvent(new AssetCreatedEvent(asset.Id, asset.FileName, asset.UserId));
        context.Assets.Add(asset);
        await context.SaveChangesAsync();

        // Assert
        var outboxMessages = await context.OutboxMessages.ToListAsync();
        outboxMessages.Should().NotBeEmpty();
        outboxMessages.Should().Contain(m => m.Type.Contains("AssetCreatedEvent"));
        
        var message = outboxMessages.First(m => m.Type.Contains("AssetCreatedEvent"));
        message.Content.Should().Contain("outbox-test.jpg");
        message.ProcessedOnUtc.Should().BeNull();
    }
}



