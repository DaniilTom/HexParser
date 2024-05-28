namespace HexParser;

public class HexLine
{
    public HexLine(string line)
    {
        Raw = line;
        Size = byte.Parse(line[1..3], System.Globalization.NumberStyles.HexNumber);
        Address = ushort.Parse(line[3..7], System.Globalization.NumberStyles.HexNumber);
        Type = short.TryParse(line[7..9], System.Globalization.NumberStyles.HexNumber, null, out short result) ?
            (RecordType)result :
            RecordType.UNSUPPORTED;

        Data = line.Substring(9, Size * 2);
        Checksum = byte.Parse(line.Substring(line.Length - 2, 2), System.Globalization.NumberStyles.HexNumber);
    }
    public byte Size { get; }
    public ushort Address { get; }
    public RecordType Type { get; }
    public string Data { get; }
    public byte[] DataBytes => Data.Chunk(2).Select(ch => byte.Parse(ch, System.Globalization.NumberStyles.HexNumber)).ToArray();
    public byte Checksum { get; }
    public string Raw { get; }
}

public enum RecordType : byte
{
    DATA = 0x00,
    EOF = 0x01,
    EXTENDED_ADDRESS = 0x04,
    UNSUPPORTED = 0xff
}