using System.Collections;
using System.IO.Compression;
using System.Text.Json.Serialization;

using Protobuf = Google.Protobuf;

namespace Mapster
{
    public interface IElement
    {
        public long Id { get; }
        public ITagList Tags { get; }

        void SetStringTable(StringTable stringTable);
    }

    namespace Elements
    {
        public class Unknown : IElement
        {
            public long Id => throw new InvalidOperationException();

            public ITagList Tags => throw new InvalidOperationException();

            public void SetStringTable(StringTable stringTable)
            {
                throw new NotImplementedException();
            }
        }
    }

}