namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    class AzureInstanceMetadataRequestMock : IAzureMetadataRequestor
    {
        private Func<IEnumerable<string>> GetAllFieldsFunc = null;
        private Func<string, string> GetSingleFieldFunc = null;

        public Dictionary<string, string> computeFields;

        public AzureInstanceMetadataRequestMock(Func<IEnumerable<string>> getAllFields = null, Func<string, string> getSingleFieldFunc = null)
        {
            this.GetAllFieldsFunc = getAllFields;
            if (getAllFields == null)
            {
                this.GetAllFieldsFunc = this.GetAllFields;
            }

            this.GetSingleFieldFunc = getSingleFieldFunc;
            if (getSingleFieldFunc == null)
            {
                this.GetSingleFieldFunc = this.GetSingleField;
            }

            computeFields = new Dictionary<string, string>();
            
        }

        public Task<string> GetAzureComputeMetadata(string fieldName)
        {
            return Task.FromResult(this.GetSingleFieldFunc(fieldName));
        }

        private string GetSingleField(string fieldName)
        {
            if (this.computeFields.ContainsKey(fieldName))
            {
                return this.computeFields[fieldName];
            }

            return string.Empty;
        }

        public Task<IEnumerable<string>> GetAzureInstanceMetadataComputeFields()
        {
            return Task.FromResult(this.GetAllFieldsFunc());
        }

        private IEnumerable<string> GetAllFields()
        {
            IEnumerable<string> fields = this.computeFields.Keys.ToArray();
            return fields;
        }
    }
}
