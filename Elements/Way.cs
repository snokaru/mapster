using System.Text.Json.Serialization;

namespace Mapster.Elements
{
    using Protobuf = Google.Protobuf;
    public class Way : IElement
    {
        private OSMPBF.Way _osmWay;
        private TagList? _tags;

        public long Id => _osmWay.Id;
        [JsonIgnore]
        public ITagList Tags => _tags == null ? throw new InvalidDataException() : _tags;

        public Way(OSMPBF.Way way)
        {
            _osmWay = way;
        }

        public void SetStringTable(StringTable stringTable)
        {
            _tags = new TagList(_osmWay.Keys, _osmWay.Vals, stringTable);
        }
    }
}