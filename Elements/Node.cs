using System.Text.Json.Serialization;

namespace Mapster.Elements
{

    public interface INode : IElement
    {
        protected const double COORDINATE_FACTOR = .000000001;

        public double Latitude { get; }
        public double Longitude { get; }
    }

    internal class SimpleNode : INode
    {
        private OSMPBF.Node _osmNode;
        private TagList? _tags;

        public long Id => _osmNode.Id;
        public double Latitude { get; }
        public double Longitude { get; }
        [JsonIgnore]
        public ITagList Tags => _tags == null ? throw new InvalidDataException() : _tags;

        public SimpleNode(OSMPBF.Node osmNode, PrimitiveBlock primitiveBlock)
        {
            _osmNode = osmNode;
            Latitude = INode.COORDINATE_FACTOR * (primitiveBlock.OffsetLatitude + ((long)primitiveBlock.Granularity * osmNode.Lat));
            Longitude = INode.COORDINATE_FACTOR * (primitiveBlock.OffsetLongitude + ((long)primitiveBlock.Granularity * osmNode.Lon));
        }

        public void SetStringTable(StringTable stringTable)
        {
            _tags = new TagList(_osmNode.Keys, _osmNode.Vals, stringTable);
        }

    }

    internal class DenseNode : INode
    {
        private class DenseTagList : ITagList
        {
            private IList<int> _keyValues;
            private int _offset;
            StringTable? _stringTable;

            public override KeyValuePair<string, string> this[int index]
            {
                get
                {
                    if (_stringTable == null)
                        return new KeyValuePair<string, string>("", "");

                    var key = _keyValues[_offset + index * 2];
                    var value = _keyValues[_offset + index * 2 + 1];
                    return new KeyValuePair<string, string>(_stringTable[key], _stringTable[value]);
                }
            }

            public override int Count { get; }

            public DenseTagList(IList<int> keyValues, int offset)
            {
                _keyValues = keyValues;
                _offset = offset;

                int count = 0;
                for (int i = offset; i < keyValues.Count && keyValues[i] != 0; ++i, ++count) ;
                Count = count / 2;
            }

            public void SetStringTable(StringTable stringTable)
            {
                _stringTable = stringTable;
            }
        }

        private DenseTagList _tags;

        public long Id { get; }
        public double Latitude { get; }
        public double Longitude { get; }
        public ITagList Tags => _tags;

        public DenseNode(OSMPBF.DenseNodes osmDenseNodes, int index, int tagOffset, long previousId, long previousLat, long previousLon, PrimitiveBlock primitiveBlock)
        {
            _tags = new DenseTagList(osmDenseNodes.KeysVals, tagOffset);

            Id = previousId + osmDenseNodes.Id[index];

            var latitude = previousLat + osmDenseNodes.Lat[index];
            Latitude = INode.COORDINATE_FACTOR * (primitiveBlock.OffsetLatitude + ((long)primitiveBlock.Granularity * latitude));

            var longitude = previousLon + osmDenseNodes.Lon[index];
            Longitude = INode.COORDINATE_FACTOR * (primitiveBlock.OffsetLongitude + ((long)primitiveBlock.Granularity * longitude));
        }

        public void SetStringTable(StringTable stringTable)
        {
            _tags?.SetStringTable(stringTable);
        }
    }
}