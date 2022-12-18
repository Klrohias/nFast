using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Klrohias.NFast.Json
{
    public class JsonWalker
    {
        private JsonTokenizer tokenizer;
        public JsonWalker(JsonTokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
            NextToken();
        }
        private JsonToken currentToken = JsonToken.None;
        public JsonToken CurrentToken => currentToken;

        public JsonToken NextToken()
        {
            return currentToken = tokenizer.ParseNextToken();
        }
        public void EnterBlock()
        {
            if (currentToken.Type != JsonTokenType.LeftBrace && currentToken.Type!=JsonTokenType.LeftBracket)
            {
                throw new Exception("unable to enter block");
            }

            NextToken();
        }

        public void LeaveBlock()
        {
            if (currentToken.Type != JsonTokenType.RightBrace && currentToken.Type != JsonTokenType.RightBracket)
            {
                throw new Exception("unable to leave block");
            }

            NextToken();
        }

        /// <summary>
        /// find the `key`, and locate to the Value of `key` if locateToValue is true
        /// </summary>
        public JsonToken LocateKey(string key, bool locateToValue = true)
        {
            while (true)
            {
                var token = currentToken;
                if (token.Type == JsonTokenType.RightBrace) return JsonToken.None;
                if (token.Type != JsonTokenType.String)
                {
                    NextToken();
                    continue;
                }
                if (token.Value != key)
                {
                    NextToken();
                    SkipBlock();
                    continue;
                }

                // make sure that it is a key
                token = NextToken();
                if (token.Type != JsonTokenType.Colon)
                {
                    continue;
                }

                if (locateToValue) NextToken();
                return token;
            }
        }

        public IEnumerable<KeyValuePair<JsonToken, JsonToken>> ReadProperties()
        {
            while (true)
            {
                var token = currentToken;
                if (token.Type == JsonTokenType.RightBrace) break;
                if (token.Type != JsonTokenType.String)
                {
                    NextToken();
                    continue;
                }

                var result = token;
                // make sure that it is a key
                token = NextToken();
                if (token.Type != JsonTokenType.Colon)
                {
                    continue;
                }

                var value = NextToken();
                yield return new KeyValuePair<JsonToken, JsonToken>(result, value);
                SkipBlock();
            }
        }

        /// <summary>
        /// get all elements of array
        /// requires: begin token point to '['
        /// requires: if you want to do something with element, make sure currentToken point to the last token of element
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public IEnumerable<JsonToken> ReadElements()
        {
            while (true)
            {
                yield return currentToken;
                NextToken();
                if (currentToken.Type == JsonTokenType.RightBracket)
                {
                    tokenizer.Goto(currentToken.Position);
                    break;
                }
                if (currentToken.Type != JsonTokenType.Comma)
                    throw new Exception($"Unexpected '{currentToken}'"); // skip ','
                NextToken();
            }
        } 

        /// <summary>
        /// extract to properties of object
        /// </summary>
        public void ExtractObject(Type type, object target, string blacklists = null, Action<string, JsonToken> blacklistCallback = null)
        {
            var properties = type.GetProperties()
                .Where(x => x.GetCustomAttribute<JsonPropertyAttribute>() != null);
            foreach (var (key, value) in ReadProperties())
            {
                var property = properties.FirstOrDefault(x =>
                    x.GetCustomAttribute<JsonPropertyAttribute>().PropertyName == key.Value);
                if (blacklists != null && blacklists.Contains(key.Value))
                {
                    blacklistCallback?.Invoke(key.Value, value);
                    continue;
                }
                if (value.Type == JsonTokenType.String)
                {
                    property.SetValue(target, value.Value);
                }
                else if (value.Type == JsonTokenType.Number)
                {
                    ExtractNumber(property, value, target);
                }
                else if (value.Type == JsonTokenType.LeftBrace)
                {
                    var obj = Activator.CreateInstance(property.PropertyType);
                    property.SetValue(target, obj);
                    ExtractObject(property.PropertyType, obj);
                } else if (value.Type == JsonTokenType.LeftBracket)
                {
                    ExtractArray(property, target);
                }
            }
        }
        private void ExtractArray(PropertyInfo property, object target)
        {
            // var type = property.PropertyType;
            // Type elementType;
            // if (type.IsGenericType)
            // {
            //     // List<Type>
            //     elementType = type.GenericTypeArguments[0];
            // }
            // else
            // {
            //     // T[]
            //     elementType = type.GetElementType();
            // }
            // EnterBlock();
            // foreach (var element in ReadElements())
            // {
            //     
            // }
            // LeaveBlock();
            throw new Exception("extracting array is not supported now");
        }
        private void ExtractNumber(PropertyInfo property, JsonToken token, object target)
        {
            var type = property.PropertyType;
            var value = token.Value;

            var method = type.GetMethod("Parse", new[] {typeof(string)});
            if (method == null) throw new Exception($"failed to cast {token} to type {type}");

            property.SetValue(target, method.Invoke(null, new object[] {value}));
        }
        public void SkipBlock(bool checkError = false)
        {
            var token = currentToken;
            if (token.Type != JsonTokenType.LeftBrace && token.Type != JsonTokenType.LeftBracket)
            {
                NextToken();
                return;
            }


            var layers = new List<uint>();
            var layerCount = 0;
            var inString = false;
            if (!checkError)
            {
                layerCount++;
                while (layerCount > 0)
                {
                    var ch = tokenizer.NextChar();
                    if (ch == '"') inString = !inString;
                    if (inString) continue;
                    switch (ch)
                    {
                        case '{':
                        case '[':
                            layerCount++;
                            break;
                        case '}':
                        case ']':
                            layerCount--;
                            break;
                    }
                }
                return;
            }
            void PushLayer(int val = 0)
            {
                var i = layerCount / 32;
                if (i + 1 > layers.Count)
                {
                    layers.Add(0);
                }

                var j = layerCount % 32;
                layers[i] |= ((uint)val << j);
                layerCount++;
            }

            int PopLayer()
            {
                var c = layerCount - 1;
                var i = c / 32;
                var j = c % 32;
                var result = layers[i] & ((uint)0b1 << j);
                layers[i] &= ~((uint) 0b1 << j);
                layerCount--;
                return result != 0 ? 1 : 0;
            }
            
            PushLayer(token.Type == JsonTokenType.LeftBrace ? 1 : 0);
            while (layerCount > 0)
            {
                var ch = tokenizer.NextChar();
                if (ch == '"') inString = !inString;
                if (inString) continue;

                if(ch == '{') PushLayer(1);
                else if (ch == '[') PushLayer(0);

                if (ch == '}' && PopLayer() == 0
                    || ch == ']' && PopLayer() == 1) throw new Exception("mismatch brackets");
            }
        }
    }
}