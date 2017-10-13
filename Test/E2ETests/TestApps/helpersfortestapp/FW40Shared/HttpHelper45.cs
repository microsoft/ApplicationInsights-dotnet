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
    using System.IO;
    using System.Net;
    using System.Runtime.InteropServices;    
    using System.Diagnostics;

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
        public static async void MakeHttpCallAsyncAwait1(string targetUrl)
        {
            try
            { 
                Uri ourUri = new Uri(targetUrl);
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
            catch (Exception ex)
            {                                
                Trace.WriteLine("Exception occured:" + ex);
            }            
        }
    }
}
