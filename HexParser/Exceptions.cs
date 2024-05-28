namespace HexParser;

public class Extended32BitHexFileException : Exception {
    public Extended32BitHexFileException(int lineNumber) : 
        base($"Hex file contains Extended Linear Address Record Type (04) at the {lineNumber}.") {}
}

public class InvalidHexFileException : Exception {
    public InvalidHexFileException(string message) :
        base(message) {}
}

public class UnsupportedRecordTypeException : Exception {
    public UnsupportedRecordTypeException() :
        base("Hex file contains unsupported record type. This library supports only 00, 01 and 04 record types.") {} 
}

public class IncorrectAdressessException : Exception {
    public IncorrectAdressessException() :
        base("End address is less than Start address") {}
}
