namespace HexParserTest;
using static HexParser.HexParser;
class Program
{
    static void Main(string[] args)
    {
        string path = "path_to_hex";

        var rawData = GetRawHexData(path);

        var sortedData16bit = GetSorted16BitHexData(path);

        var sortedData = GetSorted32BitHexData(path);

        var filtered = GetSorted32BitHexData(path, 0x1d001000, 0x1d031100);

        var fileStream = new FileStream(path, FileMode.Open);

        var fromStream = GetSorted32BitHexData(fileStream, 0x1d001000, 0x1d031100, true);

        Console.ReadLine();
    }
}
