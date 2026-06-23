using LiteDB;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.Persistence.Documents;

namespace MediaFlow.Infrastructure.Persistence;

public sealed class DeviceRepositoryAdapter : IDeviceRepository
{
    private readonly ILiteCollection<DeviceProfileDocument> _collection;

    public DeviceRepositoryAdapter(ILiteDatabase database)
    {
        _collection = database.GetCollection<DeviceProfileDocument>("devices");
        _collection.EnsureIndex(x => x.Name);
    }

    public Task<IReadOnlyList<DeviceProfile>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var profiles = _collection.FindAll().Select(ToDomain).ToList();
        return Task.FromResult<IReadOnlyList<DeviceProfile>>(profiles);
    }

    public Task<DeviceProfile?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var doc = _collection.FindById(id);
        return Task.FromResult(doc is null ? null : ToDomain(doc));
    }

    public Task SaveAsync(DeviceProfile profile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _collection.Upsert(ToDocument(profile));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _collection.Delete(id);
        return Task.CompletedTask;
    }

    // ── mapping: domain → document ────────────────────────────────────────────

    private static DeviceProfileDocument ToDocument(DeviceProfile p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        SourceFolderPath = p.SourceFolderPath,
        BackupFolderPath = p.BackupFolderPath,
        NamingTemplate = p.NamingTemplate.Select(ToDocument).ToList(),
        TelegramBotToken = p.TelegramBotToken,
        TelegramChatId = p.TelegramChatId,
        FilesPerLoad = p.FilesPerLoad
    };

    private static NamingTokenDocument ToDocument(NamingToken token) => token switch
    {
        PrefixToken p        => new() { Type = "Prefix",      Text = p.Text },
        SequenceNumberToken  => new() { Type = "Sequence" },
        CurrentDateToken     => new() { Type = "CurrentDate" },
        PhotoDateToken       => new() { Type = "PhotoDate" },
        _ => throw new InvalidOperationException($"Unknown NamingToken type: {token.GetType().Name}")
    };

    // ── mapping: document → domain ────────────────────────────────────────────

    private static DeviceProfile ToDomain(DeviceProfileDocument d) => new(
        Id:               d.Id,
        Name:             d.Name,
        SourceFolderPath: d.SourceFolderPath,
        BackupFolderPath: d.BackupFolderPath,
        NamingTemplate:   d.NamingTemplate.Select(ToDomain).ToList(),
        TelegramBotToken: d.TelegramBotToken,
        TelegramChatId:   d.TelegramChatId,
        FilesPerLoad:     d.FilesPerLoad);

    private static NamingToken ToDomain(NamingTokenDocument d) => d.Type switch
    {
        "Prefix"      => new PrefixToken(d.Text ?? ""),
        "Sequence"    => new SequenceNumberToken(),
        "CurrentDate" => new CurrentDateToken(),
        "PhotoDate"   => new PhotoDateToken(),
        _ => throw new InvalidOperationException($"Unknown NamingToken type in storage: '{d.Type}'")
    };
}
