﻿/*
 * Copyright (c) 2019 ProtocolCash
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 */

using System.Configuration;

namespace BCHSocket
{
    /// <summary>
    ///     Static config utility
    ///     - uses ConfigurationManager to read App.config file
    /// </summary>
    public static class Config
    {
        /// <summary>
        ///     Get a string property at the given key
        /// </summary>
        /// <param name="key">AppSetting key</param>
        /// <returns>string property at key</returns>
        public static string GetConfigString(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        /// <summary>
        ///     Gets a property at the given key and converts to int
        /// </summary>
        /// <param name="key">AppSetting key</param>
        /// <returns>integer at key or 0 if not a valid integer</returns>
        public static int GetConfigInt(string key)
        {
            // if valid int return same, else return 0
            return int.TryParse(ConfigurationManager.AppSettings[key], out var ret) ? ret : 0;
        }

    }
}