using System.Buffers.Binary;

// The primary namespace of the application
namespace Mapster
{
    // The class that holds the entry point to the application
    class Program
    {
        // The application entry point
        static void Main(string[] args)
        {
            // Open the local file using a Stream (https://docs.microsoft.com/en-us/dotnet/api/system.io.stream?view=net-6.0)
            using FileStream fs = new FileStream(@"", FileMode.Open);

            // Allocate enough bytes to hold an 32bit int
            byte[] buffer = new byte[4];
            // Read from the stream into the buffer
            fs.Read(buffer, 0, buffer.Length);
            // Interpret the bytes as a big-endian 32bit int (https://docs.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives?view=net-6.0)
            int headerSize = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan());

            // Alternative method of interpreting those bytes
            // Just reverse the order of the bytes in the buffer
            Array.Reverse(buffer);
            // Interpret the bytes as a 32bit little-endian int (https://docs.microsoft.com/en-us/dotnet/api/system.bitconverter?view=net-6.0)
            int headerSize2 = BitConverter.ToInt32(buffer);
        }
    }
}
