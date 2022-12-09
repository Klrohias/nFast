using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace Klrohias.NFast.ChartLoader.LargePez
{
    public class JsonTokenizer
    {
        private Stream stream;
        private UnbufferedStreamReader reader;
        public JsonTokenizer(Stream inputStream)
        {
            stream = inputStream;
            stream.Position = 0;
            reader = new UnbufferedStreamReader(stream);
        }

        private const string Whitespaces = " \t\n\r\0";
        private void SkipWhitespace()
        {
            while (Whitespaces.Contains((char) PeekChar()))
            {
                NextChar();
            }
        }

        public void Goto(long pos)
        {
            reader.Position = pos;
            // reader.DiscardBufferedData();
        }
        private long position => reader.Position;
        // private (bool has, char ch) hasPeekChar = (false, '\0');
        internal char NextChar()
        {
            if (reader.EndOfStream) throw new EndOfStreamException();
            return (char)reader.Read();
        }

        internal char PeekChar()
        {
            return (char)reader.Peek();
        }
        public JsonToken ParseNextToken()
        {
            SkipWhitespace();
            var pos = position;
            char c;
            c = NextChar();
            if (position > 2160700)
            {
                System.Diagnostics.Debug.Print("1");
            }
            switch (c)
            {
                case '"':
                {
                    var buffer = new List<byte>(128);
                    while (PeekChar() != '"')
                    {
                        buffer.Add((byte) NextChar());
                    }

                    NextChar();
                    var result = new JsonToken()
                    {
                        Type = JsonTokenType.String,
                        Value = Encoding.Default.GetString(buffer.ToArray()),
                        Position = pos
                    };
                    return result;
                }
                case '{':
                {
                    return new JsonToken()
                    {
                        Type = JsonTokenType.LeftBrace,
                        Position = pos
                    };
                }
                case '}':
                {
                    return new JsonToken()
                    {
                        Type = JsonTokenType.RightBrace,
                        Position = pos
                    };
                }
                case '[':
                {
                    return new JsonToken()
                    {
                        Type = JsonTokenType.LeftBracket,
                        Position = pos
                    };
                }
                case ']':
                {
                    return new JsonToken()
                    {
                        Type = JsonTokenType.RightBracket,
                        Position = pos
                    };
                }
                case ',':
                {
                    return new JsonToken()
                    {
                        Type = JsonTokenType.Comma,
                        Position = pos
                    };
                }
                case ':':
                {
                    return new JsonToken()
                    {
                        Type = JsonTokenType.Colon,
                        Position = pos
                    };
                }
                default:
                {
                    do
                    {
                        if (('0' <= c && c <= '9') || c == '-')
                        {
                            var buffer = "";
                            if (c == '-')
                            {
                                buffer += c;
                                c = NextChar();
                            }

                            var isSingle = false;
                            while ('0' <= c && c <= '9' || !isSingle && c == '.')
                            {
                                if (c == '.')
                                {
                                    isSingle = true;
                                }

                                buffer += c;
                                c = NextChar();
                            }

                            Goto(position - 1);
                            if (buffer == "-" || buffer.EndsWith('.'))
                            {
                                break;
                            }

                            return new JsonToken()
                            {
                                Type = JsonTokenType.Number,
                                Value = buffer
                            };
                        }

                        if ('a' <= c && c <= 'z')
                        {
                            var buffer = "";
                            while ('a' <= c && c <= 'z')
                            {
                                buffer += c;
                                c = NextChar();
                            }

                            Goto(position - 1);
                            if (buffer == "null")
                            {
                                return new JsonToken()
                                {
                                    Type = JsonTokenType.Null
                                };
                            }
                        }
                    } while (false);
                        
                    break;
                }
            }
            throw new Exception("Unexpected '" + (char) c + "' in " + position);
        }
    }

    public struct JsonToken
    {
        public JsonTokenType Type;
        public string Value;
        public long Position;
        public static JsonToken None => new JsonToken()
        {
            Type = JsonTokenType.None,
        };

        public override string ToString()
        {
            switch (Type)
            {
                case JsonTokenType.LeftBrace:
                    return "{";
                case JsonTokenType.RightBrace:
                    return "}";
                case JsonTokenType.Comma:
                    return ",";
                case JsonTokenType.LeftBracket:
                    return "[";
                case JsonTokenType.RightBracket:
                    return "]";
                case JsonTokenType.Number:
                    return Value;
                case JsonTokenType.String:
                    return $"\"{Value}\"";
                case JsonTokenType.Colon:
                    return ":";
                case JsonTokenType.Null:
                    return "null";
                default:
                    break;
            }
            return "KLROHIAS_JSON_UNKNOWN";
        }
    }

    public enum JsonTokenType
    {
        None, 
        LeftBrace,
        RightBrace,
        Comma,
        LeftBracket,
        RightBracket,
        Number,
        String,
        Colon,
        Null
    }
}

