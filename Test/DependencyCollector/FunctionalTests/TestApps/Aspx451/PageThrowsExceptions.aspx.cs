// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PageThrowsExceptions.aspx.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Aspx451
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Page that throws exception.
    /// </summary>
    [ComVisible(false)]
    public partial class PageThrowsExceptions : System.Web.UI.Page
    {
        /// <summary>
        /// Page load.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            var handledCountStr = Request.QueryString["handledCount"];
            uint handledCount;
            uint.TryParse(handledCountStr, out handledCount);
            
            var isUnhandled = Request.QueryString["isUnhandled"];

            int temp = -10;
            for (int i = 0; i < handledCount; ++i)
            {
                try
                {
                    temp = int.Parse("lala", CultureInfo.CurrentCulture);
                }
                catch (Exception)
                {
                    this.Response.Write("Exception " + i + Environment.NewLine);
                }
            }

            this.Response.Write("Temp " + temp + Environment.NewLine);

            bool needThrow;
            if (bool.TryParse(isUnhandled, out needThrow) && needThrow)
            {
                throw new WebException("Test Exception");
            }
        }
    }
}