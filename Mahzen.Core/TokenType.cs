namespace Mahzen.Core
{
    public enum TokenType: byte
    {
        Separator = (byte) '\n',
        String = (byte) '$',
        Blob = (byte) 'B',
        Error = (byte) '!',
        Integer = (byte) 'I',
        Long = (byte) 'L',
        Double = (byte) 'D',
        Null = (byte) 'N',
        False = (byte) '0',
        True = (byte) '1',
        Array = (byte) '*',
        Map = (byte) '%'
    }
}