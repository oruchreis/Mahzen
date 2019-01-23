namespace Mahzen.Core
{
    /// <summary>
    /// Every protocol object type starts with a unique byte that represents its token.
    /// </summary>
    public enum TokenType: byte
    {
        /// <summary>
        /// Default separator used in the Message Protocol
        /// </summary>
        Separator = (byte) '\n',

        /// <summary>
        /// Simple strings that does not contains any <see cref="Separator"/> token.
        /// </summary>
        String = (byte) '$',

        /// <summary>
        /// Blob of a byte array
        /// </summary>
        Blob = (byte) 'B',

        /// <summary>
        /// Error
        /// </summary>
        Error = (byte) '!',

        /// <summary>
        /// 32-bit signed int
        /// </summary>
        Integer = (byte) 'I',

        /// <summary>
        /// 64-bit signed int
        /// </summary>
        Long = (byte) 'L',

        /// <summary>
        /// Double
        /// </summary>
        Double = (byte) 'D',

        /// <summary>
        /// Null
        /// </summary>
        Null = (byte) 'N',

        /// <summary>
        /// Boolean False
        /// </summary>
        False = (byte) '0',

        /// <summary>
        /// Boolean True
        /// </summary>
        True = (byte) '1',

        /// <summary>
        /// Array
        /// </summary>
        Array = (byte) '*',

        /// <summary>
        /// Map
        /// </summary>
        Map = (byte) '%'
    }
}