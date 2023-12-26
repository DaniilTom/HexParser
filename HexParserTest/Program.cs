namespace HexParserTest;
using static HexParser.HexParser;
class Program
{
    static void Main(string[] args)
    {
        string path = "path_to_hex";

        var rawData = GetRawHexData(path);

        var sortedData = GetSorted32BitHexData(path);

        Range r = new Range(0x1d001000, 0x1d031100);

        var filtered = GetSorted32BitHexData(path, r);

        Console.WriteLine(r.End.Value);
    }
}
