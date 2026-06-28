using LiteDB;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Infrastructure.Backup;
using MediaFlow.Infrastructure.FileSystem;
using MediaFlow.Infrastructure.Loading;
using MediaFlow.Infrastructure.Metadata;
using MediaFlow.Infrastructure.Naming;
using MediaFlow.Infrastructure.Persistence;
using MediaFlow.Infrastructure.Processing;
using MediaFlow.Infrastructure.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace MediaFlow.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediaFlowServices(this IServiceCollection services)
    {
        AppPaths.EnsureCreated();

        // ── Database ──────────────────────────────────────────────────────────
        services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(AppPaths.DatabaseFile));

        // ── Shared infrastructure adapters ────────────────────────────────────
        services.AddSingleton<FileSystemAdapter>();
        services.AddSingleton<IFileService>(sp => sp.GetRequiredService<FileSystemAdapter>());

        services.AddSingleton<MetadataAdapter>();
        services.AddSingleton<IMetadataReader>(sp => sp.GetRequiredService<MetadataAdapter>());

        services.AddSingleton<IDeviceRepository, DeviceRepositoryAdapter>();
        services.AddSingleton<IThumbnailService, ThumbnailService>();
        services.AddSingleton<HttpClient>();

        // ── Pipeline stages ───────────────────────────────────────────────────
        services.AddSingleton<IRotationStage, RotationStageAdapter>();
        services.AddSingleton<IVideoConversionStage, VideoConversionStageAdapter>();
        services.AddSingleton<IBackupStage, BackupStageAdapter>();
        services.AddSingleton<ITelegramStage, TelegramAdapter>();

        // ── Media loader (tempFolderPath is not a resolvable type, use factory) ─
        services.AddSingleton<IMediaLoader>(sp => new MediaLoaderAdapter(
            sp.GetRequiredService<FileSystemAdapter>(),
            sp.GetRequiredService<MetadataAdapter>(),
            sp.GetRequiredService<IRotationStage>(),
            AppPaths.TempFolder));

        // ── Naming token resolvers (all registered; injected as IEnumerable<>) ──
        services.AddSingleton<INamingTokenResolver, PrefixTokenResolver>();
        services.AddSingleton<INamingTokenResolver, SequenceNumberTokenResolver>();
        services.AddSingleton<INamingTokenResolver, CurrentDateTokenResolver>();
        services.AddSingleton<INamingTokenResolver, PhotoDateTokenResolver>();

        // ── Application use cases ─────────────────────────────────────────────
        services.AddSingleton<BuildNamingTemplateUseCase>();
        services.AddSingleton<RegisterDeviceUseCase>();
        services.AddSingleton<EditDeviceUseCase>();
        services.AddSingleton<DeleteDeviceUseCase>();
        services.AddSingleton<GetAllDevicesUseCase>();
        services.AddSingleton<LoadMediaUseCase>();
        services.AddSingleton<RunPipelineUseCase>();

        return services;
    }
}
