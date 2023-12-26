using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq.Expressions;

namespace hexparser;

public static class HexParser
{
    public static ImmutableList<HexLine> GetRawHexData(string path)
    {

        List<HexLine> l = new List<HexLine>();

        using (StreamReader s = new StreamReader(new FileStream(path, FileMode.Open), System.Text.Encoding.ASCII))
        {
            while (!s.EndOfStream)
            {
                l.Add(new HexLine(s.ReadLine()!));
            }
        }

        if (l.Last().Type != DataType.EOF)
            throw new NotValidHexFileException("There is no EOF record at the end");

        if (l.Any(line => line.Type == DataType.UNSUPPORTED))
            throw new UnsupportedRecordTypeDetectedException();

        return l.ToImmutableList();
    }

    public static ImmutableList<HexLine> GetSorted16BitHexData(string path)
    {

        List<HexLine> l = GetRawHexData(path).ToList();

        if (l.Any(line => line.Type == DataType.EXTENDED_ADDRESS))
            throw new Extended32BitHexFileDetectedException(l.FindIndex(line => line.Type == DataType.EXTENDED_ADDRESS) + 1);

        return l.OrderBy(line => line.Address).ToImmutableList();
    }

    /// <summary>
    /// Return Dictionary, where Key is extended address, and Value is data with linear addresses
    /// </summary>
    /// <param name="path">Path to hex file</param>
    /// <returns></returns>
    public static ImmutableDictionary<ushort, ImmutableList<HexLine>> GetSorted32BitHexData(string path)
    {

        List<HexLine> lines = GetRawHexData(path).ToList();

        SortedDictionary<ushort, List<HexLine>> d = new SortedDictionary<ushort, List<HexLine>>();

        if (lines.First().Type == DataType.DATA)
            d.Add(0, new List<HexLine>());

        foreach (var extAddr in lines.Where(line => line.Type == DataType.EXTENDED_ADDRESS))
        {
            ushort extAddrParsed = ushort.Parse(extAddr.Data, System.Globalization.NumberStyles.HexNumber);
            if (!d.ContainsKey(extAddrParsed))
                d.Add(extAddrParsed, new List<HexLine>());
        }

        ushort currentExtAddress = 0x0000;
        foreach (var line in lines)
        {
            switch (line.Type)
            {
                case DataType.DATA:
                    d[currentExtAddress].Add(line);
                    break;

                case DataType.EXTENDED_ADDRESS:
                    currentExtAddress = ushort.Parse(line.Data, System.Globalization.NumberStyles.HexNumber);
                    break;

                case DataType.EOF:
                    break;
            }
        }

        foreach (var key in d.Keys.ToList())
        {
            d[key] = d[key].OrderBy(line => line.Address).ToList();
        }

        return d.ToImmutableDictionary(p => p.Key, p => p.Value.ToImmutableList());
    }

    /// <summary>
    /// Return Dictionary, where Key is extended address, and Value is data with linear addresses, limited by address range
    /// </summary>
    /// <param name="path">Path to hex file</param>
    /// <param name="range">Range of addresses (inclusive start, inclusive end)</param>
    /// <returns></returns>
    public static ImmutableDictionary<ushort, ImmutableList<HexLine>> GetSorted32BitHexData(string path, Range range)
    {
        var d = GetSorted32BitHexData(path);

        ushort extStartAddress = (ushort)((range.Start.Value & 0xffff0000) >> 16);
        ushort lineStartAddress = (ushort)(range.Start.Value & 0xffff);

        ushort extEndAddress = (ushort)((range.End.Value & 0xffff0000) >> 16);
        ushort lineEndAddress = (ushort)(range.End.Value & 0xffff);

        SortedDictionary<ushort, ImmutableList<HexLine>> sd = new SortedDictionary<ushort, ImmutableList<HexLine>>();

        foreach(var p in d)
        {
            if (p.Key == extStartAddress)
            {
                sd.Add(p.Key, p.Value.Where(line => line.Address >= lineStartAddress).ToImmutableList());
            }
            else if (p.Key > extStartAddress && p.Key < extEndAddress)
            {
                sd.Add(p.Key, p.Value);
            }
            else if (p.Key == extEndAddress)
            {
                if (lineEndAddress != 0 && p.Value.Count > 0) {
                    sd.Add(p.Key, p.Value.Where(line => line.Address <= lineEndAddress).ToImmutableList());
                }
            }
        }

        return sd.ToImmutableDictionary();
    }
}
