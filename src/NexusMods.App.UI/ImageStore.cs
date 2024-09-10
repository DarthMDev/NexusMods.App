using System.Diagnostics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Skia;
using BitFaster.Caching.Lfu;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;

namespace NexusMods.App.UI;

public sealed class ImageStore : IImageStore, IDisposable
{
    private readonly IConnection _connection;
    private ConcurrentLfu<StoredImageId, Bitmap> _bitmapCache;

    public ImageStore(IConnection connection)
    {
        _connection = connection;
        _bitmapCache = new ConcurrentLfu<StoredImageId, Bitmap>(capacity: 50);
    }

    public async ValueTask<StoredImage.ReadOnly> PutAsync(Bitmap bitmap)
    {
        using var tx = _connection.BeginTransaction();
        var storedImage = CreateStoredImage(tx, bitmap);

        var result = await tx.Commit();
        return result.Remap(storedImage);
    }

    public Bitmap? Get(OneOf<StoredImageId, StoredImage.ReadOnly> input)
    {
        if (input.TryPickT0(out var id, out var storedImage))
        {
            storedImage = StoredImage.Load(_connection.Db, id);
        }

        if (!storedImage.IsValid()) return null;
        var metadata = storedImage.Metadata;
        var bytes = storedImage.BitmapData;

        Debug.Assert((ulong)bytes.Length == metadata.DataLength);
        return _bitmapCache.GetOrAdd(id, static (_, tuple) => ToBitmap(tuple.metadata, bytes: tuple.bytes), (metadata, bytes));
    }

    public static StoredImage.New CreateStoredImage(ITransaction transaction, Bitmap bitmap)
    {
        var metadata = ToMetadata(bitmap);
        var bytes = GC.AllocateUninitializedArray<byte>(length: (int)metadata.DataLength);
        GetBitmapBytes(metadata, bitmap, bytes);

        var storedImage = new StoredImage.New(transaction)
        {
            Metadata = metadata,
            BitmapData = bytes,
        };

        return storedImage;
    }

    private static void GetBitmapBytes(ImageMetadata metadata, Bitmap bitmap, byte[] bytes)
    {
        unsafe
        {
            fixed (byte* b = bytes)
            {
                var ptr = new IntPtr(b);
                bitmap.CopyPixels(
                    sourceRect: new PixelRect(metadata.PixelSize),
                    buffer: ptr,
                    bufferSize: (int)metadata.DataLength,
                    stride: metadata.Stride
                );
            }
        }
    }

    private static Bitmap ToBitmap(ImageMetadata metadata, byte[] bytes)
    {
        unsafe
        {
            fixed (byte* b = bytes)
            {
                var ptr = new IntPtr(b);
                var bitmap = new Bitmap(
                    format: metadata.PixelFormat,
                    alphaFormat: metadata.AlphaFormat,
                    data: ptr,
                    size: metadata.PixelSize,
                    dpi: new Vector(metadata.Dpi, metadata.Dpi),
                    stride: metadata.Stride
                );

                return bitmap;
            }
        }
    }

    private static ImageMetadata ToMetadata(Bitmap bitmap)
    {
        var width = (uint)bitmap.PixelSize.Width;
        var height = (uint)bitmap.PixelSize.Height;

        if (!bitmap.Format.HasValue) throw new NotSupportedException("Bitmap doesn't have a PixelFormat");
        var format = bitmap.Format.Value;

        if (!bitmap.AlphaFormat.HasValue) throw new NotSupportedException("Bitmap doesn't have an AlphaFormat");
        var alphaFormat = bitmap.AlphaFormat.Value;

        var dpi = bitmap.Dpi;
        var x = (int)Math.Floor(dpi.X);
        var y = (int)Math.Floor(dpi.Y);
        if (x != y) throw new NotSupportedException($"Uneven DPI isn't supported: `{dpi.ToString()}`");

        // NOTE(erri120): small hack, Avalonia PixelFormat struct is sealed, and we can't really do anything with it.
        // Instead, we'll just convert to SkColorType using the method provided by Avalonia.
        var skColorType = format.ToSkColorType();
        skColorType.ToPixelFormat();

        var metadata = new ImageMetadata(
            imageWidth: width,
            imageHeight: height,
            skColorType: skColorType,
            alphaFormat: alphaFormat,
            dpi: (uint)x
        );

        return metadata;
    }

    private bool _isDisposed;
    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        foreach (var kv in _bitmapCache.ToArray())
        {
            kv.Value.Dispose();
        }

        _bitmapCache = null!;
        _isDisposed = true;
    }
}

