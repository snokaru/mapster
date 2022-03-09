
namespace Mapster.Elements
{
    public class ChangeSet : IElement
    {
        public long Id { get; }

        public ITagList Tags => throw new NotImplementedException();

        public void SetStringTable(StringTable stringTable)
        {
            throw new NotImplementedException();
        }
    }
}