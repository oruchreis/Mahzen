using System;

namespace Mahzen.Core
{
    /// <summary>
    /// Protocol Builder Interface
    /// </summary>
    public interface IProtocolWriter
    {
        /// <summary>
        /// Writes protocol object
        /// </summary>
        /// <param name="protocolObject"></param>
        void Write(MessageProtocolObject protocolObject);

        /// <summary>
        /// Writes string value 
        /// </summary>
        /// <param name="value"></param>
        void Write(string value);

        /// <summary>
        /// Writes integer value
        /// </summary>
        /// <param name="value"></param>
        void Write(int value);

        /// <summary>
        /// Writes double value
        /// </summary>
        /// <param name="value"></param>
        void Write(double value);

        /// <summary>
        /// Writes long value
        /// </summary>
        /// <param name="value"></param>
        void Write(long value);

        /// <summary>
        /// Writes boolean value
        /// </summary>
        /// <param name="value"></param>
        void Write(bool value);

        /// <summary>
        /// Writes an error.
        /// </summary>
        /// <param name="error"></param>
        void WriteError(Error error);

        /// <summary>
        /// Writes an error.
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        void WriteError(string errorCode, string errorMessage);

        /// <summary>
        /// Write null value
        /// </summary>
        void WriteNull();

        /// <summary>
        /// Writes an array.
        /// </summary>
        /// <param name="arrayItemBuilders"></param>
        void Write(params Action<IProtocolWriter>[] arrayItemBuilders);

        /// <summary>
        /// Writes a map.
        /// </summary>
        /// <param name="mapItemBuilders"></param>
        void Write(params (Action<IProtocolWriter> KeyBuilder, Action<IProtocolWriter> ValueBuilder)[] mapItemBuilders);

        /// <summary>
        /// Helper method to create an array by begin-end methods. Can be used with using.
        /// </summary>
        /// <returns></returns>
        ArrayProtocolWriter BeginArray();

        /// <summary>
        /// Helper method to create a map by begin-end methods. Can be used with using.
        /// </summary>
        /// <returns></returns>
        MapProtocolWriter BeginMap();

        /// <summary>
        /// Converts command to message protocol objects and writes them.
        /// </summary>
        /// <param name="command"></param>
        void Write(Command command);
    }
}
