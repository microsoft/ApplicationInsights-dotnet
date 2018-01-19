namespace Microsoft.ApplicationInsights.WindowsServer.Mock
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;

    internal class AzureInstanceMetadataRequestMock : IAzureMetadataRequestor
    {
        public Dictionary<string, string> ComputeFields;

        private Func<IEnumerable<string>> getAllFieldsFunc = null;
        private Func<string, string> getSingleFieldFunc = null;

        public AzureInstanceMetadataRequestMock(Func<IEnumerable<string>> getAllFields = null, Func<string, string> getSingleFieldFunc = null)
        {
            this.getAllFieldsFunc = getAllFields;
            if (getAllFields == null)
            {
                this.getAllFieldsFunc = this.GetAllFields;
            }

            this.getSingleFieldFunc = getSingleFieldFunc;
            if (getSingleFieldFunc == null)
            {
                this.getSingleFieldFunc = this.GetSingleField;
            }

            this.ComputeFields = new Dictionary<string, string>();
        }

        public Task<string> GetAzureComputeMetadata(string fieldName)
        {
            return Task.FromResult(this.getSingleFieldFunc(fieldName));
        }

        public Task<IEnumerable<string>> GetAzureInstanceMetadataComputeFields()
        {
            return Task.FromResult(this.getAllFieldsFunc());
        }

        private string GetSingleField(string fieldName)
        {
            if (this.ComputeFields.ContainsKey(fieldName))
            {
                return this.ComputeFields[fieldName];
            }

            return string.Empty;
        }

        private IEnumerable<string> GetAllFields()
        {
            IEnumerable<string> fields = this.ComputeFields.Keys.ToArray();
            return fields;
        }
    }
}
