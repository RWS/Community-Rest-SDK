﻿using System;
using System.Collections.Generic;
using Telligent.Evolution.RestSDK.Exceptions;
using Telligent.Evolution.RestSDK.Extensions;

namespace Telligent.Evolution.RestSDK.Json
{
    public class JsonParser
    {
        public static string ReadAsString(char[] json)
        {
            var start = 0;
            var end = -1;
            var previous = JsonChar.Empty;
            var found = false;

            for (var pos = 0; pos < json.Length; pos++)
            {
                var current = json[pos];

                if (end == -1 && (current == JsonChar.DoubleQuote || char.IsWhiteSpace(current)))
                {
                    start++;
                    continue;
                }

                end = pos;

                if (start > 0 && current == JsonChar.DoubleQuote && previous != JsonChar.Escape)
                {
                    found = true;
                    break;
                }

                previous = current;
            }

            return !found ? string.Empty : ToString(json, start, end);
        }

        public static double? ReadAsNumber(char[] json)
        {
            return ReadAsPrimative(json, n => Convert.ToDouble(n)) as int?;
        }

        public static bool? ReadAsBoolean(char[] json)
        {
            return ReadAsPrimative(json, b => Convert.ToBoolean(b)) as bool?;
        }

        public static string ReadAsObject(char[] json)
        {
            var start = 0;
            var end = -1;
            var previous = JsonChar.Empty;
            var braces = new Stack<char>();
            var inString = false;
            var found = false;

            for (var pos = 0; pos < json.Length; pos++)
            {
                var current = json[pos];

                if (current == JsonChar.DoubleQuote && previous != JsonChar.Escape) inString = !inString;
                if (current == JsonChar.OpenBrace) braces.Push(current);

                if (end == -1 && char.IsWhiteSpace(current))
                {
                    start++;
                    continue;
                }

                end = pos;

                if (!inString && current == JsonChar.CloseBrace)
                {
                    if (braces.Count > 0) braces.Pop();
                    if (braces.Count == 0)
                    {
                        found = true;
                        end++;
                        break;
                    }
                }

                previous = current;
            }

            if (!found) throw new JsonBadFormat("No closing brace found: \r\n" + new string(json));

            return ToString(json, start, end);
        }

        public static IEnumerable<string> ReadAsArray(char[] json)
        {
            json = TrimArray(json);

            var start = 0;
            var end = -1;
            var previous = JsonChar.Empty;
            var inString = false;
            var inObject = false;
            var inArray = false;
            var inPrimative = false;
            var braces = new Stack<char>();
            var brackets = new Stack<char>();

            int length;

            for (var pos = 0; pos < json.Length; pos++)
            {
                var current = json[pos];

                bool isIn = inObject || inArray || inPrimative;

                if (!isIn && current == JsonChar.DoubleQuote && previous != JsonChar.Escape)
                {
                    inString = !inString;
                    start = inString ? pos : start;
                }

                isIn = isIn || inString;

                if (current == JsonChar.OpenBrace)
                {
                    if (!isIn)
                        start = pos;

                    inObject = true;
                    braces.Push(current);
                }

                isIn = isIn || inObject;

                if (current == JsonChar.OpenBracket)
                {
                    if (!isIn)
                        start = pos;

                    inArray = true;
                    brackets.Push(current);
                }

                isIn = isIn || inArray;

                if (!isIn && (current == JsonChar.True || current == JsonChar.False || current == JsonChar.Null || char.IsNumber(current)))
                {
                    start = pos;
                    inPrimative = true;
                }

                if (inObject && current == JsonChar.CloseBrace)
                {
                    if (braces.Count > 0) braces.Pop();
                    inObject = braces.Count > 0;
                }

                if (inArray && current == JsonChar.CloseBracket)
                {
                    if (brackets.Count > 0) brackets.Pop();
                    inArray = brackets.Count > 0;
                }

                if (inPrimative && (char.IsWhiteSpace(current) || current == JsonChar.Delimitter)) inPrimative = false;

                isIn = isIn || inPrimative;

                end = pos;

                if (!isIn && IsCloser(current))
                {
                    length = end - start;
                    if (length > 0) yield return new string(json, start, length);

                    start = pos + 1;
                }

                previous = current;
            }

            length = ++end - start;
            if (length > 0) yield return new string(json, start, length);
        }

        private static char[] TrimArray(char[] json)
        {
            var start = 0;
            var end = -1;
            var previous = JsonChar.Empty;
            var bracket = new Stack<char>();
            var inString = false;
            var found = false;

            for (var pos = 0; pos < json.Length; pos++)
            {
                var current = json[pos];

                if (current == JsonChar.DoubleQuote && previous != JsonChar.Escape) inString = !inString;
                if (!inString && current == JsonChar.OpenBracket) bracket.Push(current);

                if (end == -1 && (char.IsWhiteSpace(current) || current == JsonChar.OpenBracket))
                {
                    start++;
                    continue;
                }

                end = pos;

                if (start > 0 && !inString && current == JsonChar.CloseBracket)
                {
                    if (bracket.Count > 0) bracket.Pop();
                    if (bracket.Count == 0)
                    {
                        found = true;
                        break;
                    }
                }

                previous = current;
            }

            if (!found) throw new JsonBadFormat("No closing bracket found: \r\n" + new string(json));

            return json.SubArray(start, end);
        }

        private static object ReadAsPrimative(char[] json, Func<string, object> convert)
        {
            var start = 0;
            var end = -1;
            var found = false;

            for (var pos = 0; pos < json.Length; pos++)
            {
                var current = json[pos];

                if (end == -1 && char.IsWhiteSpace(current))
                {
                    start++;
                    continue;
                }

                end = pos;

                if (IsCloser(current))
                {
                    found = true;
                    break;
                }
            }

            if (!found) throw new JsonBadFormat("No closing token found: \r\n" + new string(json));

            var primative = ToString(json, start, end);

            return string.IsNullOrEmpty(primative) ? null : convert(primative.Trim());
        }

        private static bool IsCloser(char current)
        {
            switch (current)
            {
                case JsonChar.CloseBrace:
                case JsonChar.CloseBracket:
                case JsonChar.Delimitter:
                    return true;
                default:
                    return false;
            }
        }

        private static string ToString(char[] json, int start, int end)
        {
            var length = end - start;
            return length > 0 ? new string(json, start, length) : string.Empty;
        }
    }
}
