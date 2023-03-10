using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using WebApp.Controllers;

namespace WebApp.Services
{
    // NuGet\Install-Package Microsoft.Azure.Devices -Version 1.38.2
    public class ApplianceService
    {
        private readonly IConfiguration _configuration;
        string _connectionString;
        string _targetDeviceId;
        
        public ApplianceService(IConfiguration configuration)
        {
            _configuration = configuration;

            _connectionString = _configuration.GetConnectionString("IoTHub");
            _targetDeviceId = _configuration.GetSection("DeviceSettings:DeviceId").Get<string>();

        }

        public async Task<ApplianceStatus> GetApplianceStatusAsync()
        {
            using (var serviceClient = ServiceClient.CreateFromConnectionString(_connectionString, TransportType.Amqp))
            {

                var method = new CloudToDeviceMethod("status", TimeSpan.FromSeconds(10));
                
                var methodResult = await serviceClient.InvokeDeviceMethodAsync(_targetDeviceId, method);
                var payload = methodResult.GetPayloadAsJson();
                var applianceResponse = JsonConvert.DeserializeObject<ApplianceResponse>(payload);

                return applianceResponse == null ? ApplianceStatus.Unknown : applianceResponse.Status;

            }
        }

        public async Task<ApplianceStatus> ToggleApplianceStatusAsync()
        {

            using (var serviceClient = ServiceClient.CreateFromConnectionString(_connectionString, TransportType.Amqp))
            {
                var method = new CloudToDeviceMethod("toggle", TimeSpan.FromSeconds(10));

                var methodResult = await serviceClient.InvokeDeviceMethodAsync(_targetDeviceId, method);
                var applianceResponse = JsonConvert.DeserializeObject<ApplianceResponse>(methodResult.GetPayloadAsJson());

                return applianceResponse == null ? ApplianceStatus.Unknown : applianceResponse.Status;

            }
        }

    }

    public enum ApplianceStatus
    {
        On = 1,
        Off = 0,
        Unknown = -1
    }


    public class ApplianceResponse
    {
        public ApplianceStatus Status { get; set; }
    }

   
}
