using System.Collections;

namespace Mapster
{
    using Tag = KeyValuePair<string, string>;

    public class TagList : ITagList
    {
        private IList<uint> _keys;
        private IList<uint> _values;
        private StringTable _stringTable;

        public override Tag this[int index]
        {
            get
            {
                var key = (int)_keys[index];
                var value = (int)_values[index];
                return new Tag(_stringTable[key], _stringTable[value]);
            }
        }

        public override int Count => _keys.Count;

        public TagList(IList<uint> keys, IList<uint> values, StringTable stringTable)
        {
            _keys = keys;
            _values = values;
            _stringTable = stringTable;
        }

    }
}