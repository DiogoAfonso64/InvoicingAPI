using Microsoft.Extensions.Caching.Memory;

namespace Invoyz.Api.Infrastructure.Common;

public abstract class CacheRepositoryBase<TEntity> where TEntity : class
{
    private readonly IMemoryCache _cache;
    private readonly string _keyPrefix;
    private readonly object _indexLock = new();

    protected CacheRepositoryBase(IMemoryCache cache, string keyPrefix)
    {
        _cache = cache;
        _keyPrefix = keyPrefix;
    }

    protected Task<TEntity?> GetEntryAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_cache.Get<TEntity>(EntryKey(id)));

    protected Task<IReadOnlyList<TEntity>> GetAllEntriesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TEntity> entities;
        lock (_indexLock)
        {
            entities = GetIndex()
                .Select(id => _cache.Get<TEntity>(EntryKey(id)))
                .Where(entity => entity is not null)
                .Select(entity => entity!)
                .ToList();
        }

        return Task.FromResult(entities);
    }

    protected Task SetEntryAsync(Guid id, TEntity entity, CancellationToken cancellationToken = default)
    {
        _cache.Set(EntryKey(id), entity);

        lock (_indexLock)
        {
            GetIndex().Add(id);
        }

        return Task.CompletedTask;
    }

    protected Task RemoveEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _cache.Remove(EntryKey(id));

        lock (_indexLock)
        {
            GetIndex().Remove(id);
        }

        return Task.CompletedTask;
    }

    private HashSet<Guid> GetIndex()
        => _cache.GetOrCreate(IndexKey, _ => new HashSet<Guid>())!;

    private string EntryKey(Guid id) => $"{_keyPrefix}:{id}";

    private string IndexKey => $"{_keyPrefix}:__index";
}
