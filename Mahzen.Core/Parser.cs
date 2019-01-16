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
     * Single Command Request: <array>
     *     - Single Command Array:
     *         0: <command_keyword_string>
     *         1-n: parameters
     * Single Command Response: <mmp_type>
     *
     * Pipelining Commands: We can send multiple commands separating with \n byte:  
     *     Multi Command Request: <single_command_request>\n<single_command_request>         
     *     Multi Command Response: <array_of_mmp_types>
     * 
     * MMP Types:
     * - Blob:          $<length>\n<bytes>\n
     * - Errors:        !<length>\n<error_code>\n<error_message_utf8_string>\n
     * - Integer:       i<integer_4_bytes>\n
     * - Long:          l<long_8_bytes>\n
     * - Double:        d<double_8_bytes>\n
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
        private int _currentPosition;
        public Parser(Span<byte> buffer)
        {
            _buffer = buffer;
            _currentPosition = 0;
        }

        public List<MessageProtocolData> Parse()
        {
            //EOB: End of Buffer
            
            var result = new List<MessageProtocolData>();
            //first byte is important
            while (ReadNextByte(out var firstByte))
            {
                var statement = ReadStatement(firstByte);
                if (statement == null) 
                    return result; //EOB
                
                result.Add(statement);
            }

            return result;
        }

        public Span<byte> RemeaningBuffer => _buffer.Slice(_currentPosition);

        public void Reset(Span<byte> nextBuffer)
        {
            var newBuffer = new Span<byte>(new byte[RemeaningBuffer.Length + nextBuffer.Length]);
            RemeaningBuffer.CopyTo(newBuffer.Slice(0, RemeaningBuffer.Length));
            nextBuffer.CopyTo(newBuffer.Slice(RemeaningBuffer.Length));
            _buffer = newBuffer;
            _currentPosition = 0;
        }

        private MessageProtocolData ReadStatement(byte firstByte)
        {
            var commandStartPosition = _currentPosition - 1;
            switch (firstByte)
            {
                #region Blob

                case (byte) TokenType.Blob:
                {
                    if (!ReadInteger(out var length))
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

                    return new BlobProtocolData()
                    {
                        Bytes = bytes.ToArray()
                    };
                }

                #endregion

                #region Error

                case (byte) TokenType.Error:
                {
                    if (!ReadInteger(out var length))
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

                    return new ErrorProtocolData()
                    {
                        Code = errorCode,
                        Message = errorMessage
                    };
                }

                #endregion

                #region Integer

                case (byte) TokenType.Integer:
                {
                    if (!ReadInteger(out var value))
                    {
                        //EOB 
                        _currentPosition = commandStartPosition;
                        return null;
                    }

                    return new IntegerProtocolData()
                    {
                        Value = value
                    };
                }

                #endregion

                #region Long

                case (byte) TokenType.Long:
                {
                    if (!ReadLong(out var value))
                    {
                        //EOB 
                        _currentPosition = commandStartPosition;
                        return null;
                    }

                    return new LongProtocolData()
                    {
                        Value = value
                    };
                }

                #endregion

                #region Double

                case (byte) TokenType.Double:
                {
                    if (!ReadDouble(out var value))
                    {
                        //EOB 
                        _currentPosition = commandStartPosition;
                        return null;
                    }

                    return new DoubleProtocolData()
                    {
                        Value = value
                    };
                }

                #endregion

                #region Null

                case (byte) TokenType.Null:
                {
                    if (!ExpectSeparator())
                    {
                        //EOB
                        _currentPosition = commandStartPosition;
                        return null;
                    }

                    return new NullProtocolData();
                }

                #endregion

                #region Boolean

                case (byte) TokenType.True:
                {
                    if (!ExpectSeparator())
                    {
                        //EOB
                        _currentPosition = commandStartPosition;
                        return null;
                    }

                    return new BooleanProtocolData
                    {
                        Value = true
                    };
                }

                case (byte) TokenType.False:
                {
                    if (!ExpectSeparator())
                    {
                        //EOB
                        _currentPosition = commandStartPosition;
                        return null;
                    }

                    return new BooleanProtocolData
                    {
                        Value = false
                    };
                }

                #endregion

                #region Array

                case (byte) TokenType.Array:
                {
                    if (!ReadInteger(out var count))
                    {
                        //EOB
                        _currentPosition = commandStartPosition;
                        return null;
                    }

                    var items = new List<MessageProtocolData>(count);
                    for (var i = 0; i < count; i++)
                    {
                        if (!ReadNextByte(out var arrayItemFirstByte))
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        var item = ReadStatement(arrayItemFirstByte); 
                        if (item == null)
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        items[i] = item;
                    }
                    
                    return new ArrayProtocolData
                    {
                        Items = items
                    };
                }

                #endregion

                #region Map

                case (byte) TokenType.Map:
                {
                    if (!ReadInteger(out var count))
                    {
                        //EOB
                        _currentPosition = commandStartPosition;
                        return null;
                    }

                    var items = new Dictionary<MessageProtocolData, MessageProtocolData>(count);
                    for (var i = 0; i < count; i++)
                    {
                        if (!ReadNextByte(out var keyFirstByte))
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        var key = ReadStatement(keyFirstByte); 
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

                        var value = ReadStatement(valueFirstByte); 
                        if (value == null)
                        {
                            //EOB
                            _currentPosition = commandStartPosition;
                            return null;
                        }

                        items[key] = value;
                    }
                    
                    return new MapProtocolData
                    {
                        Items = items 
                    };
                }

                #endregion
                
                
                default:
                    throw new SyntaxErrorException("Invalid start character at {0}.", _currentPosition);
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

        private bool ReadInteger(out int result)
        {
            result = int.MinValue;
            if (_currentPosition + sizeof(int) + 1 >= _buffer.Length)
                return false;            
                
            result = MemoryMarshal.Cast<byte, int>(_buffer.Slice(_currentPosition, sizeof(int)))[0];            
            _currentPosition += sizeof(int);
            
            if (!ExpectSeparator())
                return false;
            
            return true;
        }
        
        private bool ReadLong(out long result)
        {
            result = long.MinValue;
            if (_currentPosition + sizeof(long) + 1 >= _buffer.Length)
                return false;            
                
            result = MemoryMarshal.Cast<byte, long>(_buffer.Slice(_currentPosition, sizeof(long)))[0];            
            _currentPosition += sizeof(long);
            
            if (!ExpectSeparator())
                return false;
            
            return true;
        }
        
        private bool ReadDouble(out double result)
        {
            result = double.MinValue;
            if (_currentPosition + sizeof(double) + 1 >= _buffer.Length)
                return false;            
                
            result = MemoryMarshal.Cast<byte, double>(_buffer.Slice(_currentPosition, sizeof(double)))[0];            
            _currentPosition += sizeof(double);
            
            if (!ExpectSeparator())
                return false;
            
            return true;
        }

        private bool ExpectSeparator()
        {
            if (!ReadNextByte(out var lastByte))
            {
                //EOB
                return false;
            }
            if (lastByte != (byte) TokenType.Separator)
                throw new SyntaxErrorException("Invalid character at {0}. Expecting '\\n' separator", _currentPosition);

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
            
            result = encoding.GetString(_buffer.Slice(_currentPosition, length));
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