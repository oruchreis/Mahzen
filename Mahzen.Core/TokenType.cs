namespace Mahzen.Core
{
    public enum TokenType: byte
    {
        Separator = (byte) '\n',
        Blob = (byte) '$',
        Error = (byte) '!',
        Integer = (byte) 'i',
        Long = (byte) 'l',
        Double = (byte) 'd',
        Null = (byte) 'N',
        False = (byte) '0',
        True = (byte) '1',
        Array = (byte) '*',
        Map = (byte) '%',
        Nested,
        Value
    }
}