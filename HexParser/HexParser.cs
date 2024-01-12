﻿using System.Collections.Immutable;

namespace HexParser;

public static class HexParser
{
    public static string[] CommentStartSequences = { ";", "//" };

    /// <summary>
    /// Parse hex file to collection of HexLine objects represent every string
    /// </summary>
    /// <param name="stream">Data stream contains hex</param>
    /// <param name="leaveStreamOpen">True to leave stream open, default: false</param>
    /// <returns></returns>
    /// <exception cref="InvalidHexFileException">Throw when 'weak' validation failed: no EOF record, no semicolon ':' in line, empty lines</exception>
    /// <exception cref="UnsupportedRecordTypeException">Throw when unsupported Record Type detected (valid only 00, 01 and 04)</exception>
    public static ImmutableList<HexLine> GetRawHexData(Stream stream, bool leaveStreamOpen = false)
    {
        List<string> lines = new List<string>();

        using (StreamReader s = new StreamReader(stream, System.Text.Encoding.ASCII, leaveOpen: leaveStreamOpen))
        {
            while (!s.EndOfStream)
            {
                var line = s.ReadLine();

                lines.Add(line);
            }
        }

        List<HexLine> hexLines = new List<HexLine>();
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
                throw new InvalidHexFileException($"Empty line at position {hexLines.Count() + 1}");

            if (CommentStartSequences.Any(startSequence => line.StartsWith(startSequence)))
                continue;

            if (!line.Contains(":"))
                throw new InvalidHexFileException($"Line {hexLines.Count() + 1} does not contains start symbol ':' : '{line}'");

            try
            {
                var hexLine = new HexLine(line.Substring(line.IndexOf(':')));
                hexLines.Add(hexLine);

                if (hexLine.Type == RecordType.EOF)
                    break;
            }
            catch (Exception e)
            {
                throw new InvalidHexFileException($"Line {hexLines.Count() + 1} can not be parsed: '{line}'");
            }
        }

        if (hexLines.Last().Type != RecordType.EOF)
            throw new InvalidHexFileException("There is no EOF record at the end");

        if (hexLines.Any(line => line.Type == RecordType.UNSUPPORTED))
            throw new UnsupportedRecordTypeException();

        return hexLines.ToImmutableList();
    }

    /// <summary>
    /// Parse hex file to collection of HexLine objects represent every string
    /// </summary>
    /// <param name="path">Path to hex file</param>
    /// <returns></returns>
    public static ImmutableList<HexLine> GetRawHexData(string path)
    {
        return GetRawHexData(new FileStream(path, FileMode.Open));
    }

    /// <summary>
    /// Parse 16-bit address hex file to collection of HexLine objects represent every string sorted by address
    /// </summary>
    /// <param name="stream">Data stream contains hex</param>
    /// <param name="leaveStreamOpen">True to leave stream open, default: false</param>
    /// <returns></returns>
    /// <exception cref="Extended32BitHexFileException">Throw when 32-bit address hex file detected (presence Record Type 04)</exception>
    public static ImmutableList<HexLine> GetSorted16BitHexData(Stream stream, bool leaveStreamOpen = false)
    {
        List<HexLine> l = GetRawHexData(stream, leaveStreamOpen).ToList();

        if (l.Any(line => line.Type == RecordType.EXTENDED_ADDRESS))
            throw new Extended32BitHexFileException(l.FindIndex(line => line.Type == RecordType.EXTENDED_ADDRESS) + 1);

        return l.OrderBy(line => line.Address).ToImmutableList();
    }

    public static ImmutableList<HexLine> GetSorted16BitHexData(string path)
    {
        return GetSorted16BitHexData(new FileStream(path, FileMode.Open));
    }

    /// <summary>
    /// Parse 32-bit address hex file to dictionary, where Key is Extended Adress and Value is list of linear address hex string
    /// </summary>
    /// <param name="stream">Data stream contains hex</param>
    /// <param name="leaveStreamOpen">True to leave stream open, default: false</param>
    /// <returns></returns>
    public static ImmutableDictionary<ushort, ImmutableList<HexLine>> GetSorted32BitHexData(Stream stream, bool leaveStreamOpen = false)
    {
        List<HexLine> lines = GetRawHexData(stream, leaveStreamOpen).ToList();

        SortedDictionary<ushort, List<HexLine>> d = new SortedDictionary<ushort, List<HexLine>>();

        if (lines.First().Type == RecordType.DATA)
            d.Add(0, new List<HexLine>());

        foreach (var extAddr in lines.Where(line => line.Type == RecordType.EXTENDED_ADDRESS))
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
                case RecordType.DATA:
                    d[currentExtAddress].Add(line);
                    break;

                case RecordType.EXTENDED_ADDRESS:
                    currentExtAddress = ushort.Parse(line.Data, System.Globalization.NumberStyles.HexNumber);
                    break;

                case RecordType.EOF:
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
    /// Parse 32-bit address hex file to dictionary, where Key is Extended Adress and Value is list of linear address hex string
    /// </summary>
    /// <param name="path">Path to hex file</param>
    /// <returns></returns>
    public static ImmutableDictionary<ushort, ImmutableList<HexLine>> GetSorted32BitHexData(string path)
    {
        return GetSorted32BitHexData(new FileStream(path, FileMode.Open));
    }

    /// <summary>
    /// Parse 32-bit address hex file to dictionary, where Key is Extended Adress and Value is list of linear address hex string, limited by address range (INCLUSIVE)
    /// </summary>
    /// <param name="stream">Data stream contains hex</param>
    /// <param name="startAddress">Inclusive start address</param>
    /// <param name="endAddress">Inclusive end address</param>
    /// <param name="leaveStreamOpen">True to leave stream open, default: false</param>
    /// <returns></returns>
    public static ImmutableDictionary<ushort, ImmutableList<HexLine>> GetSorted32BitHexData(Stream stream, int startAddress, int endAddress, bool leaveStreamOpen = false)
    {
        var d = GetSorted32BitHexData(stream, leaveStreamOpen);

        ushort extStartAddress = (ushort)((startAddress & 0xffff0000) >> 16);
        ushort lineStartAddress = (ushort)(startAddress & 0xffff);

        ushort extEndAddress = (ushort)((endAddress & 0xffff0000) >> 16);
        ushort lineEndAddress = (ushort)(endAddress & 0xffff);

        SortedDictionary<ushort, ImmutableList<HexLine>> sd = new SortedDictionary<ushort, ImmutableList<HexLine>>();

        foreach (var p in d)
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
                if (lineEndAddress != 0 && p.Value.Count > 0)
                {
                    sd.Add(p.Key, p.Value.Where(line => line.Address <= lineEndAddress).ToImmutableList());
                }
            }
        }

        return sd.ToImmutableDictionary();
    }

    /// <summary>
    /// Parse 32-bit address hex file to dictionary, where Key is Extended Adress and Value is list of linear address hex string, limited by address range (INCLUSIVE)
    /// </summary>
    /// <param name="path">Data stream contains hex</param>
    /// <param name="startAddress">Inclusive start address</param>
    /// <param name="endAddress">Inclusive end address</param>
    /// <returns></returns>
    public static ImmutableDictionary<ushort, ImmutableList<HexLine>> GetSorted32BitHexData(string path, int startAddress, int endAddress)
    {
        return GetSorted32BitHexData(new FileStream(path, FileMode.Open), startAddress, endAddress);
    }
}
