// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpHelper45.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Shared HTTP helper class to make outbound http calls for DOT NET FW 4.0
// </summary>

namespace FW45Shared
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Runtime.InteropServices;
    using FW40Shared;

    /// <summary>
    /// Contains static methods to help make outbound http calls
    /// </summary>
    [ComVisible(false)]
    public class HttpHelper45
    {
        /// <summary>
        /// Make async http calls.
        /// Async, Await keywords are used to achieve async calls. 
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        /// <param name="hostname">the hostname to which connection is to be made</param>      
        public static async void MakeHttpCallAsyncAwait1(int count, string hostname)
        {
            for (int i = 0; i < count; i++)
            {
                Uri ourUri = new Uri(string.Format(CultureInfo.InvariantCulture, "https://www.{0}.com", hostname));
                WebRequest wr = WebRequest.Create(ourUri);
                var response = await wr.GetResponseAsync();
                using (var stm = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stm))
                    {
                        var content = await reader.ReadToEndAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Make async http calls.
        /// Async, Await keywords are used to achieve async calls. 
        /// </summary>        
        /// <param name="count">no of calls to be made</param>                
        public static async void MakeHttpCallAsyncAwait1Failed(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Uri ourUri = new Uri(HttpHelper40.UrlWhichThrowException);
                WebRequest wr = WebRequest.Create(ourUri);
                var response = await wr.GetResponseAsync();
                using (var stm = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stm))
                    {
                        var content = await reader.ReadToEndAsync();
                    }
                }
            }
        }
    }
}
