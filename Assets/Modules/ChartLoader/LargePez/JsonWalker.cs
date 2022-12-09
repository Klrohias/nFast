using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Klrohias.NFast.ChartLoader.LargePez
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

        private JsonToken NextToken()
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

        public IEnumerable<JsonToken> ReadElements()
        {
            return null;
        }

        /// <summary>
        /// extract to properties of object
        /// </summary>
        public void ExtractObject(Type type, object target)
        {
            var properties = type.GetProperties()
                .Where(x => x.GetCustomAttribute<JsonPropertyAttribute>() != null);
            foreach (var (key, value) in ReadProperties())
            {
                var property = properties.FirstOrDefault(x =>
                    x.GetCustomAttribute<JsonPropertyAttribute>().PropertyName == key.Value);

                if (value.Type == JsonTokenType.String)
                {
                    property.SetValue(target, value.Value);
                }
                else if (value.Type == JsonTokenType.Number)
                {
                    SetNumberProperty(property, value, target);
                }
                else if (value.Type == JsonTokenType.LeftBrace)
                {
                    var obj = Activator.CreateInstance(property.PropertyType);
                    property.SetValue(target, obj);
                    ExtractObject(property.PropertyType, obj);
                } else if (value.Type == JsonTokenType.LeftBracket)
                {
                    throw new NotImplementedException("array is not supported now");
                }
            }
        }

        private void SetNumberProperty(PropertyInfo property, JsonToken token, object target)
        {
            var type = property.PropertyType;
            var value = token.Value;

            var method = type.GetMethod("Parse", new[] {typeof(string)});
            if (method == null) throw new Exception($"failed to cast {token} to type {type}");

            property.SetValue(target, method.Invoke(null, new object[] {value}));
        }
        public void SkipBlock()
        {
            var token = currentToken;
            if (token.Type != JsonTokenType.LeftBrace && token.Type != JsonTokenType.LeftBracket)
            {
                return;
            }


            var layers = new List<uint>();
            var layerCount = 0;

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
            bool inString = false;
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