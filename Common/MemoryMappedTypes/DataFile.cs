using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mapster.Common.MemoryMappedTypes
{
    /// <summary>
    /// Action to be called when iterating over <see cref="MapFeature"/> in a given bounding box via a call to <see cref="DataFile.IterateOverFeatures(BoundingBox, MapFeatureDelegate)"/>
    /// </summary>
    /// <param name="feature">The current <see cref="MapFeature"/>.</param>
    /// <param name="label">The label of the feature, <see cref="string.Empty"/> if not available.</param>
    /// <param name="coordinates">The coordinates of the <see cref="MapFeature"/>.</param>
    /// <returns></returns>
    public delegate bool MapFeatureDelegate(MapFeature feature, string label, ReadOnlySpan<Coordinate> coordinates);

    /// <summary>
    /// Represents a file with map data organized in the following format:<br/>
    /// 
    /// <see cref="FileHeader"/><br/>
    /// Array of <see cref="TileHeaderEntry"/> with <see cref="FileHeader.TileCount"/> records<br/>
    /// Array of tiles, each tile organized:<br/>
    /// <see cref="TileBlockHeader"/><br/>
    /// Array of <see cref="MapFeature"/> with <see cref="TileBlockHeader.FeaturesCount"/> at offset <see cref="TileHeaderEntry.OffsetInBytes"/> + size of <see cref="TileBlockHeader"/> in bytes.<br/>
    /// Array of <see cref="Coordinate"/> with <see cref="TileBlockHeader.CoordinatesCount"/> at offset <see cref="TileBlockHeader.CharactersOffsetInBytes"/>.<br/>
    /// Array of <see cref="StringEntry"/> with <see cref="TileBlockHeader.StringCount"/> at offset <see cref="TileBlockHeader.StringsOffsetInBytes"/>.<br/>
    /// Array of <see cref="char"/> with <see cref="TileBlockHeader.CharactersCount"/> at offset <see cref="TileBlockHeader.CharactersOffsetInBytes"/>.<br/>
    /// </summary>
    public unsafe class DataFile : IDisposable
    {
        private readonly int MapFeatureSizeInBytes = Marshal.SizeOf<MapFeature>();
        private readonly int CoordinateSizeInBytes = Marshal.SizeOf<Coordinate>();
        private readonly int TileHeaderEntrySizeInBytes = Marshal.SizeOf<TileHeaderEntry>();
        private readonly int StringEntrySizeInBytes = Marshal.SizeOf<StringEntry>();
        private readonly int TileBlockHeaderSizeInBytes = Marshal.SizeOf<TileBlockHeader>();
        private readonly int FileHeaderSizeInBytes = Marshal.SizeOf<FileHeader>();

        private byte* _ptr;
        private MemoryMappedViewAccessor _mma;
        private MemoryMappedFile _mmf;

        private FileHeader* _fileHeader;

        public DataFile(string path)
        {
            _mmf = MemoryMappedFile.CreateFromFile(path);
            _mma = _mmf.CreateViewAccessor();
            _mma.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
            _fileHeader = (FileHeader*)_ptr;
        }

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _mma?.SafeMemoryMappedViewHandle.ReleasePointer();
                    _mma?.Dispose();
                    _mmf?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private TileHeaderEntry* GetNthTileHeader(int i)
        {
            return (TileHeaderEntry*)(_ptr + i * TileHeaderEntrySizeInBytes + FileHeaderSizeInBytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private (TileBlockHeader? Tile, ulong TileOffset) GetTile(int tileId)
        {
            ulong tileOffset = 0;
            for (var i = 0; i < _fileHeader->TileCount; ++i)
            {
                var tileHeaderEntry = GetNthTileHeader(i);
                if (tileHeaderEntry->ID == tileId)
                {
                    tileOffset = tileHeaderEntry->OffsetInBytes;
                    return (*(TileBlockHeader*)(_ptr + tileOffset), tileOffset);
                }
            }

            return (null, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private MapFeature* GetFeature(int i, ulong offset)
        {
            return (MapFeature*)(_ptr + offset + TileBlockHeaderSizeInBytes + i * MapFeatureSizeInBytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private ReadOnlySpan<Coordinate> GetCoordinates(ulong coordinateOffset, ulong tileOffset, int ithCoordinate, int coordinateCount)
        {
            return new ReadOnlySpan<Coordinate>((_ptr + tileOffset + coordinateOffset + ithCoordinate * CoordinateSizeInBytes), coordinateCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private string GetString(ulong stringsOffset, ulong charsOffset, ulong tileOffset, int i)
        {
            var stringEntry = (StringEntry*)(_ptr + tileOffset + stringsOffset + i * StringEntrySizeInBytes);
            return new ReadOnlySpan<char>((_ptr + tileOffset + charsOffset + stringEntry->Offset * 2), stringEntry->Length).ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void IterateOverFeatures(BoundingBox b, MapFeatureDelegate action)
        {
            if (action == null) return;
            var tiles = TiligSystem.GetTilesForBoundingBox(b.MinLat, b.MinLon, b.MaxLat, b.MaxLon);
            for (var i = 0; i < tiles.Length; ++i)
            {
                var header = GetTile(tiles[i]);
                if (header.Tile == null) continue;
                for (var j = 0; j < header.Tile.Value.FeaturesCount; ++j)
                {
                    var feature = GetFeature(j, header.TileOffset);
                    var coordinates = GetCoordinates(header.Tile.Value.CoordinatesOffsetInBytes, header.TileOffset, feature->CoordinateOffset, feature->CoordinateCount);
                    bool isFeatureInBBox = false;

                    for (var k = 0; k < coordinates.Length; ++k)
                    {                                               
                        if (b.Contains(coordinates[k]))
                        {
                            isFeatureInBBox = true;
                        }
                    }

                    var label = feature->LabelOffset < 0 ? string.Empty : GetString(header.Tile.Value.StringsOffsetInBytes, header.Tile.Value.CharactersOffsetInBytes, header.TileOffset, feature->LabelOffset);

                    if (isFeatureInBBox)
                    {
                        var result = action(*feature, label, coordinates);
                        if (!result) break;
                    }
                }
            }
        }        
    }
}
