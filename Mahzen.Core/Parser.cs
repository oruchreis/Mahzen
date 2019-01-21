using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Mahzen.Core
{
    /* Mahzen Message Protocol(MMP) Format
     * 
     * This format is similar to RESP format,
     * but I have changed some parts as a design decision
     * 
     * MMP Types:
     * - SimpleString:  $<utf8_bytes_without_'\n'>\n
     * - Blob:          B<length>\n<bytes>\n
     * - Errors:        !<length>\n<error_code>\n<error_message_utf8_string>\n
     * - Integer:       I<integer_4_bytes>\n
     * - Long:          L<long_8_bytes>\n
     * - Double:        D<double_8_bytes>\n
     * - Null:          N\n
     * - Boolean:       0\n or 1\n
     * - Array:         *<count>\n<items>        =>items can be any type
     * - Map:           %<count>\n<key><value>   =>key and values can be any type
     *
     * length: int, 4 bytes
     * error_code: ascii string, 8 bytes
     * count: int, 4 bytes
     * 
     */
    public ref struct Parser
    {
        private Span<byte> _buffer;
        private Memory<MessageProtocolObject> _result;
        private int _resultIndex;
        private int _currentPosition;
        private Action _resizeResult;

        public Parser(Span<byte> buffer, Memory<MessageProtocolObject> result, Action resizeResult = null)
        {
            _buffer = buffer;
            _result = result;
            _currentPosition = 0;
            _resultIndex = 0;
            _resizeResult = resizeResult;
        }

        public void Parse()
        {
            //EOB: End of Buffer

            var span = _result.Span;
            //first byte is important
            while (ReadNextByte(out var firstByte))
            {
                try
                {
                    var commandStartPosition = _currentPosition - 1;
                    var protocolObject = ReadProtocolObject(firstByte);
                    if (protocolObject == null)
                        return; //EOB
                    if (_resultIndex + 1 >= span.Length) //result is full, need more space.
                    {
                        if (_resizeResult == null)
                            throw new BufferOverflowException("The result buffer given to parser is not enough to hold parser results. Use resizeResult parameter to expand the buffer.");
                        _resizeResult();
                    }

                    span[_resultIndex++] = protocolObject;
                }
                catch (SyntaxErrorException e)
                {
                    var isTokenType = Enum.IsDefined(typeof(TokenType), firstByte);
                    if (isTokenType)
                        throw new SyntaxErrorException($"Invalid {(TokenType)firstByte:g} type format. See inner exception for detail.", _currentPosition, e);
                    else
                        throw e;
                }
            }
        }

        public Span<byte> RemeaningBuffer => _buffer.Slice(_currentPosition);
        public int ResultIndex => _resultIndex;

        public void SlideBuffer(Span<byte> nextBuffer)
        {
            var newBuffer = new Span<byte>(new byte[RemeaningBuffer.Length + nextBuffer.Length]);
            RemeaningBuffer.CopyTo(newBuffer.Slice(0, RemeaningBuffer.Length));
            nextBuffer.CopyTo(newBuffer.Slice(RemeaningBuffer.Length));
            _buffer = newBuffer;
            _currentPosition = 0;
        }

        private MessageProtocolObject ReadProtocolObject(byte firstByte)
        {
            var commandStartPosition = _currentPosition - 1;
            switch (firstByte)
            {
                #region Simple String
                case (byte)TokenType.String:
                    {
                        if (!ReadUntil(new[] { (byte)TokenType.Separator }, out var bytes))
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        return new StringProtocolObject()
                        {
                            Value = Encoding.UTF8.GetString(bytes)
                        };
                    }
                #endregion
                #region Blob
                case (byte)TokenType.Blob:
                    {
                        if (!ReadInteger(out var length))
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ReadBytes(length, out var bytes))
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        return new BlobProtocolObject()
                        {
                            Bytes = bytes.ToArray()
                        };
                    }

                #endregion

                #region Error

                case (byte)TokenType.Error:
                    {
                        if (!ReadInteger(out var length))
                        {
                            //EOB 
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ReadAsciiString(8, out var errorCode))
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ReadUtf8String(length, out var errorMessage))
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        return new ErrorProtocolObject()
                        {
                            Code = errorCode,
                            Message = errorMessage
                        };
                    }

                #endregion

                #region Integer

                case (byte)TokenType.Integer:
                    {
                        if (!ReadInteger(out var value))
                        {
                            //EOB 
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        return new IntegerProtocolObject()
                        {
                            Value = value
                        };
                    }

                #endregion

                #region Long

                case (byte)TokenType.Long:
                    {
                        if (!ReadLong(out var value))
                        {
                            //EOB 
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        return new LongProtocolObject()
                        {
                            Value = value
                        };
                    }

                #endregion

                #region Double

                case (byte)TokenType.Double:
                    {
                        if (!ReadDouble(out var value))
                        {
                            //EOB 
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        return new DoubleProtocolObject()
                        {
                            Value = value
                        };
                    }

                #endregion

                #region Null

                case (byte)TokenType.Null:
                    {
                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        return new NullProtocolObject();
                    }

                #endregion

                #region Boolean

                case (byte)TokenType.True:
                    {
                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        return new BooleanProtocolObject
                        {
                            Value = true
                        };
                    }

                case (byte)TokenType.False:
                    {
                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        return new BooleanProtocolObject
                        {
                            Value = false
                        };
                    }

                #endregion

                #region Array

                case (byte)TokenType.Array:
                    {
                        if (!ReadInteger(out var count))
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        var items = new MessageProtocolObject[count];
                        for (var i = 0; i < count; i++)
                        {
                            if (!ReadNextByte(out var arrayItemFirstByte))
                            {
                                //EOB
                                _currentPosition = commandStartPosition;
                                return null;
                            }

                            var item = ReadProtocolObject(arrayItemFirstByte);
                            if (item == null)
                            {
                                //EOB
                                _currentPosition = commandStartPosition;
                                return null;
                            }

                            items[i] = item;
                        }

                        return new ArrayProtocolObject
                        {
                            Items = items
                        };
                    }

                #endregion

                #region Map

                case (byte)TokenType.Map:
                    {
                        if (!ReadInteger(out var count))
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        if (!ExpectSeparator())
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        var items = new KeyValuePair<MessageProtocolObject, MessageProtocolObject>[count];
                        for (var i = 0; i < count; i++)
                        {
                            if (!ReadNextByte(out var keyFirstByte))
                            {
                                //EOB
                                _currentPosition = commandStartPosition;
                                return null;
                            }

                            var key = ReadProtocolObject(keyFirstByte);
                            if (key == null)
                            {
                                //EOB
                                _currentPosition = commandStartPosition;
                                return null;
                            }

                            if (!ReadNextByte(out var valueFirstByte))
                            {
                                //EOB
                                _currentPosition = commandStartPosition;
                                return null;
                            }

                            var value = ReadProtocolObject(valueFirstByte);
                            if (value == null)
                            {
                                //EOB
                                _currentPosition = commandStartPosition;
                                return null;
                            }

                            items[i] = new KeyValuePair<MessageProtocolObject, MessageProtocolObject>(key, value);
                        }

                        return new MapProtocolObject
                        {
                            Items = items
                        };
                    }

                #endregion


                default:
                    throw new SyntaxErrorException($"Invalid start character #{firstByte} at {{0}}.", _currentPosition);
            }
        }

        private bool ReadNextByte(out byte result)
        {
            result = byte.MinValue;
            if (_currentPosition >= _buffer.Length)
                return false;

            result = _buffer[_currentPosition++];
            return true;
        }

        private bool ReadUntil(Span<byte> searchBytes, out Span<byte> result)
        {
            var lastPos = _buffer.Slice(_currentPosition).IndexOf(searchBytes);
            if (lastPos == -1)
            {
                result = Span<byte>.Empty;
                return false;
            }

            result = _buffer.Slice(_currentPosition, lastPos);
            _currentPosition += lastPos + 1;
            return true;
        }

        private bool ReadInteger(out int result)
        {
            result = int.MinValue;
            if (_currentPosition + sizeof(int) + 1 >= _buffer.Length)
                return false;

            result = MemoryMarshal.Cast<byte, int>(_buffer.Slice(_currentPosition, sizeof(int)))[0];
            _currentPosition += sizeof(int);

            return true;
        }

        private bool ReadLong(out long result)
        {
            result = long.MinValue;
            if (_currentPosition + sizeof(long) + 1 >= _buffer.Length)
                return false;

            result = MemoryMarshal.Cast<byte, long>(_buffer.Slice(_currentPosition, sizeof(long)))[0];
            _currentPosition += sizeof(long);

            return true;
        }

        private bool ReadDouble(out double result)
        {
            result = double.MinValue;
            if (_currentPosition + sizeof(double) + 1 >= _buffer.Length)
                return false;

            result = MemoryMarshal.Cast<byte, double>(_buffer.Slice(_currentPosition, sizeof(double)))[0];
            _currentPosition += sizeof(double);

            return true;
        }

        private bool ExpectSeparator()
        {
            if (!ReadNextByte(out var lastByte))
            {
                //EOB
                return false;
            }
            if (lastByte != (byte)TokenType.Separator)
                throw new SyntaxErrorException($"Invalid character '{lastByte}' at {{0}}. Expecting '\\n' separator", _currentPosition);

            return true;
        }

        private bool ReadBytes(int length, out Span<byte> bytes)
        {
            if (_currentPosition + length >= _buffer.Length)
            {
                bytes = Span<byte>.Empty;
                return false;
            }

            bytes = _buffer.Slice(_currentPosition, length);
            _currentPosition += length;
            return true;
        }

        private bool ReadString(int length, Encoding encoding, out string result)
        {
            if (_currentPosition + length >= _buffer.Length)
            {
                result = string.Empty;
                return false;
            }

            result = encoding.GetString(_buffer.Slice(_currentPosition, length)).TrimEnd('\0');
            _currentPosition += length;
            return true;
        }

        private bool ReadUtf8String(int length, out string result)
        {
            return ReadString(length, Encoding.UTF8, out result);
        }

        private bool ReadAsciiString(int length, out string result)
        {
            return ReadString(length, Encoding.ASCII, out result);
        }
    }
}