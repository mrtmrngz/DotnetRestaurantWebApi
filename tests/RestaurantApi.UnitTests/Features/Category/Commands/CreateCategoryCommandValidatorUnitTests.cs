using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RestaurantApi.Application.Common.Abstractions;
using RestaurantApi.Application.Common.Abstractions.Repositories;
using RestaurantApi.Application.Common.Abstractions.Services;
using RestaurantApi.Application.Features.Category.Commands.CreateCategoryCommand;
using RestaurantApi.Application.Features.Files.Dtos;

namespace RestaurantApi.UnitTests.Features.Category.Commands;

public class CreateCategoryCommandValidatorUnitTests
{
    [Fact]
    public async Task Handle_WhenDatabaseFails_ShouldDeleteUploadedFileAndThrowException()
    {
        var fileStorage = Substitute.For<IFileStorage>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mediaRepository = Substitute.For<IMediaRepository>();
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var slugService = Substitute.For<ISlugService>();
        var cacheService = Substitute.For<ICacheService>();
        var logger = Substitute.For<ILogger<CreateCategoryCommandHandler>>();

        var fakePublicId = "media/test_123.png";
        var fakeFormFile = Substitute.For<IFormFile>();

        fileStorage.UploadAsync(Arg.Any<IFormFile>())
            .Returns(Task.FromResult(new UploadFileResult { PublicId = fakePublicId }));

        unitOfWork.CommitTransactionAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database connection lost!"));

        var handler = new CreateCategoryCommandHandler(
            logger,
            cacheService,
            unitOfWork,
            fileStorage,
            categoryRepository,
            mediaRepository,
            slugService);

        var command = new CreateCategoryCommand(Title: "Test Category", Image: fakeFormFile); 

        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));

        await fileStorage.Received(1).DeleteAsync(fakePublicId);
    }
}