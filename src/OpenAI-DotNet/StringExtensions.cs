﻿using System.Net.Http;
using System.Text;

namespace OpenAI
{
    internal static class StringExtensions
    {
        public static StringContent ToJsonStringContent(this string json)
        {
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}
