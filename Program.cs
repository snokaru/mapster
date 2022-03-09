using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Compression;
using Google.Protobuf;

namespace Mapster
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var blob in new PBFFile("<osm_pbf_file>.osm.pbf"))
            {
                switch (blob.Type)
                {
                    case BlobType.Primitive:
                        {
                            var nodes = blob.ToPrimitiveBlock().Where(pg => pg.ContainedType == PrimitiveGroup.ElementType.Node).Select(pg => pg.Where(n => n.Tags.Count > 0).ToArray()).First();
                            System.Text.Json.JsonSerializer.Serialize<IElement[]>(Console.OpenStandardOutput(), nodes, new System.Text.Json.JsonSerializerOptions()
                            {
                                WriteIndented = true
                            });
                            break;
                        }
                }
                Console.WriteLine();
            }
        }
    }
}
