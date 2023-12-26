namespace hexparser;

public class Extended32BitHexFileDetectedException : Exception {
    public Extended32BitHexFileDetectedException(int lineNumber) : 
        base($"Hex file contains Extended Linear Address Record Type (04) at the {lineNumber}.") {}
}

public class NotValidHexFileException : Exception {
    public NotValidHexFileException(string message) :
        base(message) {}
}

public class UnsupportedRecordTypeDetectedException : Exception {
    public UnsupportedRecordTypeDetectedException() :
        base("Hex file contains unsupported record type. This library supports only 00, 01 and 04 record types.") {} 
}