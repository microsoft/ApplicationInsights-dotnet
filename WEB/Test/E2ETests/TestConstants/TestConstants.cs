namespace TestUtils.TestConstants
{
    public class TestConstants
    {
        public const string WebAppInstrumentationKey = "e45209bb-49ab-41a0-8065-793acb3acc56";
        public const string WebAppCore20NameInstrumentationKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8";
        public const string WebAppCore30NameInstrumentationKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8";
        public const string WebApiInstrumentationKey = "0786419e-d901-4373-902a-136921b63fb2";

        public const string WebAppName = "WebApp";
        public const string WebAppCore20Name = "WebAppCore20";
        public const string WebAppCore30Name = "WebAppCore30";
        public const string WebApiName = "WebApi";
        public const string IngestionName = "Ingestion";

        public const string WebAppImageName = "e2etests_e2etestwebapp";
        public const string WebAppCore20ImageName = "e2etests_e2etestwebappcore20";
        public const string WebAppCore30ImageName = "e2etests_e2etestwebappcore30";
        public const string WebApiImageName = "e2etests_e2etestwebapi";
        public const string IngestionImageName = "e2etests_ingestionservice";

        public const string WebAppContainerName = WebAppImageName + "_1";
        public const string WebAppCore20ContainerName = WebAppCore20ImageName + "_1";
        public const string WebAppCore30ContainerName = WebAppCore30ImageName + "_1";
        public const string WebApiContainerName = WebApiImageName + "_1";
        public const string IngestionContainerName = IngestionImageName + "_1";

        public const string WebAppHealthCheckPath = "/Dependencies?type=etw";
        public const string WebAppCore20HealthCheckPath = "/api/values";
        public const string WebAppCore30HealthCheckPath = "/api/values";
        public const string WebApiHealthCheckPath = "/api/values";
        public const string IngestionHealthCheckPath = "/api/Data/HealthCheck?name=cijo";

        public const string WebAppFlushPath = "/Dependencies?type=flush";
        public const string WebAppCore20FlushPath = "/external/calls?type=flush";
        public const string WebAppCore30FlushPath = "/external/calls?type=flush";
        public const string WebApiFlushPath = "/api/values";
        public const string IngestionFlushPath = "/api/Data/HealthCheck?name=cijo";

        public const string WebAppTargetNameToWebApi = "e2etestwebapi";
        public const string WebAppTargetNameToInvalidHost = "abcdefzzzzeeeeadadad.com";
        public const string WebAppUrlToInvalidHost = "http://abcdefzzzzeeeeadadad.com";        
        public const string WebAppUrlToWebApiSuccess = "http://e2etestwebapi:80/api/values";
        public const string WebAppUrlToWebApiException = "http://e2etestwebapi:80/api/values/999";

        public const string WebAppTargetNameToSql = "sql-server | dependencytest";
        public const string WebAppFullQueryToSqlException = "WAITFOR DELAY '00:00:00:007';SELECT name FROM master.dbo.sysdatabasesunknown";
        public const string WebAppFullQueryToSqlSuccess = "WAITFOR DELAY '00:00:00:007';select * from dbo.Messages";
        public const string WebAppFullQueryCountToSqlSuccess = "WAITFOR DELAY '00:00:00:007';select count(*) from dbo.Messages";        
        public const string WebAppStoredProcedureNameToSql = "GetTopTenMessages";
        public const string WebAppFullQueryToSqlSuccessXML = "WAITFOR DELAY '00:00:00:007';select * from dbo.Messages FOR XML AUTO";        

        public const string WebAppTargetToEmulatorBlob = "e2etests_azureemulator_1:10000";
        public const string WebAppTargetToEmulatorQueue = "e2etests_azureemulator_1:10001";
        public const string WebAppTargetToEmulatorTable = "e2etests_azureemulator_1:10002";
    }
}
