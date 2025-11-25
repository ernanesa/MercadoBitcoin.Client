using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace MercadoBitcoin.Client.Internal.Pooling;

/// <summary>
/// Centralized object pool manager for high-performance scenarios.
/// Provides pooled instances of frequently created objects to reduce GC pressure.
/// </summary>
internal static class ObjectPoolManager
{
    #region StringBuilder Pool

    private static readonly ObjectPool<StringBuilder> _stringBuilderPool =
        new DefaultObjectPoolProvider { MaximumRetained = 32 }.CreateStringBuilderPool(256, 8192);

    /// <summary>
    /// Rents a StringBuilder from the pool.
    /// </summary>
    public static StringBuilder RentStringBuilder() => _stringBuilderPool.Get();

    /// <summary>
    /// Returns a StringBuilder to the pool.
    /// </summary>
    public static void ReturnStringBuilder(StringBuilder stringBuilder)
    {
        if (stringBuilder.Capacity > 8192)
        {
            // Don't return overly large builders to the pool
            return;
        }
        stringBuilder.Clear();
        _stringBuilderPool.Return(stringBuilder);
    }

    #endregion

    #region List Pool

    private static readonly ConcurrentDictionary<Type, object> _listPools = new();

    /// <summary>
    /// Rents a List of the specified type from the pool.
    /// </summary>
    public static List<T> RentList<T>()
    {
        var pool = (ObjectPool<List<T>>)_listPools.GetOrAdd(typeof(T), _ =>
            new DefaultObjectPoolProvider { MaximumRetained = 16 }.Create(new ListPooledObjectPolicy<T>()));
        return pool.Get();
    }

    /// <summary>
    /// Returns a List to the pool.
    /// </summary>
    public static void ReturnList<T>(List<T> list)
    {
        if (list.Capacity > 1024)
        {
            // Don't return overly large lists
            return;
        }
        list.Clear();
        var pool = (ObjectPool<List<T>>)_listPools.GetOrAdd(typeof(T), _ =>
            new DefaultObjectPoolProvider { MaximumRetained = 16 }.Create(new ListPooledObjectPolicy<T>()));
        pool.Return(list);
    }

    private sealed class ListPooledObjectPolicy<T> : IPooledObjectPolicy<List<T>>
    {
        public List<T> Create() => new(64);
        public bool Return(List<T> obj)
        {
            obj.Clear();
            return obj.Capacity <= 1024;
        }
    }

    #endregion

    #region Dictionary Pool

    private static readonly ConcurrentDictionary<(Type, Type), object> _dictPools = new();

    /// <summary>
    /// Rents a Dictionary of the specified types from the pool.
    /// </summary>
    public static Dictionary<TKey, TValue> RentDictionary<TKey, TValue>() where TKey : notnull
    {
        var pool = (ObjectPool<Dictionary<TKey, TValue>>)_dictPools.GetOrAdd((typeof(TKey), typeof(TValue)), _ =>
            new DefaultObjectPoolProvider { MaximumRetained = 16 }.Create(new DictionaryPooledObjectPolicy<TKey, TValue>()));
        return pool.Get();
    }

    /// <summary>
    /// Returns a Dictionary to the pool.
    /// </summary>
    public static void ReturnDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict) where TKey : notnull
    {
        if (dict.Count > 256)
        {
            // Don't return overly large dictionaries
            return;
        }
        dict.Clear();
        var pool = (ObjectPool<Dictionary<TKey, TValue>>)_dictPools.GetOrAdd((typeof(TKey), typeof(TValue)), _ =>
            new DefaultObjectPoolProvider { MaximumRetained = 16 }.Create(new DictionaryPooledObjectPolicy<TKey, TValue>()));
        pool.Return(dict);
    }

    private sealed class DictionaryPooledObjectPolicy<TKey, TValue> : IPooledObjectPolicy<Dictionary<TKey, TValue>> where TKey : notnull
    {
        public Dictionary<TKey, TValue> Create() => new(32);
        public bool Return(Dictionary<TKey, TValue> obj)
        {
            obj.Clear();
            return true;
        }
    }

    #endregion

    #region MemoryStream Pool

    private static readonly ObjectPool<MemoryStream> _memoryStreamPool =
        new DefaultObjectPoolProvider { MaximumRetained = 16 }.Create(new MemoryStreamPooledObjectPolicy());

    /// <summary>
    /// Rents a MemoryStream from the pool.
    /// </summary>
    public static MemoryStream RentMemoryStream() => _memoryStreamPool.Get();

    /// <summary>
    /// Returns a MemoryStream to the pool.
    /// </summary>
    public static void ReturnMemoryStream(MemoryStream stream)
    {
        if (stream.Capacity > 65536)
        {
            // Don't return overly large streams
            return;
        }
        stream.SetLength(0);
        stream.Position = 0;
        _memoryStreamPool.Return(stream);
    }

    private sealed class MemoryStreamPooledObjectPolicy : IPooledObjectPolicy<MemoryStream>
    {
        public MemoryStream Create() => new(4096);
        public bool Return(MemoryStream obj)
        {
            if (obj.Capacity > 65536)
                return false;
            obj.SetLength(0);
            obj.Position = 0;
            return true;
        }
    }

    #endregion

    #region Byte Array Pool (Wrapper for ArrayPool)

    /// <summary>
    /// Rents a byte array from the shared ArrayPool.
    /// </summary>
    public static byte[] RentByteArray(int minimumLength) => ArrayPool<byte>.Shared.Rent(minimumLength);

    /// <summary>
    /// Returns a byte array to the shared ArrayPool.
    /// </summary>
    public static void ReturnByteArray(byte[] array, bool clearArray = false)
    {
        ArrayPool<byte>.Shared.Return(array, clearArray);
    }

    #endregion

    #region Char Array Pool (Wrapper for ArrayPool)

    /// <summary>
    /// Rents a char array from the shared ArrayPool.
    /// </summary>
    public static char[] RentCharArray(int minimumLength) => ArrayPool<char>.Shared.Rent(minimumLength);

    /// <summary>
    /// Returns a char array to the shared ArrayPool.
    /// </summary>
    public static void ReturnCharArray(char[] array, bool clearArray = false)
    {
        ArrayPool<char>.Shared.Return(array, clearArray);
    }

    #endregion
}

/// <summary>
/// RAII-style wrapper for pooled StringBuilder usage.
/// </summary>
internal ref struct PooledStringBuilder
{
    private StringBuilder? _builder;

    public PooledStringBuilder()
    {
        _builder = ObjectPoolManager.RentStringBuilder();
    }

    public StringBuilder Builder => _builder ?? throw new ObjectDisposedException(nameof(PooledStringBuilder));

    public override string ToString() => _builder?.ToString() ?? string.Empty;

    public void Dispose()
    {
        if (_builder != null)
        {
            ObjectPoolManager.ReturnStringBuilder(_builder);
            _builder = null;
        }
    }
}

/// <summary>
/// RAII-style wrapper for pooled List usage.
/// </summary>
internal ref struct PooledList<T>
{
    private List<T>? _list;

    public PooledList()
    {
        _list = ObjectPoolManager.RentList<T>();
    }

    public List<T> List => _list ?? throw new ObjectDisposedException(nameof(PooledList<T>));

    public void Dispose()
    {
        if (_list != null)
        {
            ObjectPoolManager.ReturnList(_list);
            _list = null;
        }
    }
}

/// <summary>
/// RAII-style wrapper for pooled MemoryStream usage.
/// </summary>
internal ref struct PooledMemoryStream
{
    private MemoryStream? _stream;

    public PooledMemoryStream()
    {
        _stream = ObjectPoolManager.RentMemoryStream();
    }

    public MemoryStream Stream => _stream ?? throw new ObjectDisposedException(nameof(PooledMemoryStream));

    public void Dispose()
    {
        if (_stream != null)
        {
            ObjectPoolManager.ReturnMemoryStream(_stream);
            _stream = null;
        }
    }
}
