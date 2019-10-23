// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestWebForm.aspx.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// --------------------------------------------------------------------------------------------------------------------

namespace WebAppFW45.Aspx
{
    using System;

    public partial class TestWebForm : System.Web.UI.Page
    {
        private const string Generate500ParameterName = "ifThrow";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (null != this.Request.Params[Generate500ParameterName])
            {
                throw new InvalidOperationException();
            }
        }
    }
}