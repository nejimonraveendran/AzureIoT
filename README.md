# Build a Smart Switch Using Azure IoT Hub, Espressif ESP32, and ASP.NET Core

Internet of Things (IoT) and Azure are 2 areas I love to play around. Several years ago, I built a smart switch using [ESP8266](https://en.wikipedia.org/wiki/ESP8266) and [CloudMQTT](https://www.cloudmqtt.com/). I was aware of the existence of IoT services in Azure, but I never got a chance to try it hands on, so I thought why not rebuild the same solution using Azure IoT Hub! 

The purpose of this article is to document the hands-on experience and learnings obtained from implementing the solution, but this time with an improved and more secure design.  The technologies used are [Azure IoT Hub](https://azure.microsoft.com/en-us/products/iot-hub#overview), [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core?view=aspnetcore-7.0), and [Espressif ESP32](https://www.espressif.com/en/products/socs/esp32) microcontroller. If you follow this tutorial, you will basically build a solution to securely control (on/off) any electric appliance (e.g., a home light) remotely through your smartphone or computer. 

## The Problem
**The requirement/user story:**  As a the owner of the appliance, I, the user, want to turn on/off the appliance remotely through my phone/laptop, with the following conditions:  
- I should be able to control the appliance even while I am away from home.
- The communication between my application and the device controlling the appliance should be secure, i.e., encrypted with TLS 1.2.
- Access to the ON/OFF button should be able to be controlled by proper authentication and authorization.

## The Solution
The solution architecture looks like the following:
![image](https://user-images.githubusercontent.com/68135957/223001314-045b2ff0-0edc-40b1-9ab3-202e3b8e67f9.png)

There are 3 main modules in the solution:
1. **Azure IoT Hub:**  The IoT messaging hub provisioned in Azure.   
2. **ASP.NET Core MVC Web Application:**  The user-facing web application, publishing "on/off" messages to the Azure IoT Hub.
3. **Espressif ESP32 Microcontroller:** The physical IoT device at home, subscribing to "on/off" messages from Azure IoT Hub.

**The solution will work like this:** The ASP.NET Core MVC Application will be hosted as an [Azure App Service Web App](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore?tabs=net60&pivots=development-environment-vs) with a public URL, which can be accessed using any modern browser.  The application's home page will show an ON/OFF button.  When the button is clicked, the web application will publish an "on/off" message to the Azure IoT Hub. On the other side, an [Espressif ESP32](https://en.wikipedia.org/wiki/ESP32) microcontroller at home has already established a connection to the Azure IoT Hub and beeen subscribing to "on/off" messages from the hub.  Upon reception of the message from the web application, the IoT Hub will pass it to the device, and the device will act accordingly, i.e., turn on or off the appliance through an electromagnetic relay.  In addition, the device will report the current status of the appliance back to the web application through the IoT Hub so that the user will receive immediate feedback about the success/failure of the action.        

## About Azure IoT Hub
[Azure IoT Hub](https://learn.microsoft.com/en-us/azure/iot-hub/iot-concepts-and-iot-hub) is a fully managed PaaS solution that functions as a messaging hub between applications and physical IoT devices. There are several messaging patterns that the IoT Hub supports.  They are mainly:
- **Device to Cloud (D2C):**  In this pattern, an IoT device asynchronously sends messages to IoT Hub.  This is also known as Telemetry.
- **Cloud to Device (C2D):**  A cloud application asynchronously sends messages to the physical device.
- **Direct Method:**  This is also a C2D message but done in a synchronous manner.  Read more about Direct Methods [here](https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods).

Since the nature of the D2C and C2D above is asynchronous, they are more suitable for fire-and-forget use cases, where we do not need an immediate response.  In our use case, it is desired to get an immediate response regarding if the appliance was successfully turned on or off.  For this reason, the best pattern to use is Direct Method. 

Azure IoT Hub supports several communication protocols as well (HTTPS, AMQP, WebSockets, MQTT, etc.).  In any IoT-based solution, probably the most widely used communication protocol is [MQTT](https://en.wikipedia.org/wiki/MQTT). MQTT is a publish-subscribe pattern, where devices can publish messages as "topics" to an MQTT server (often called a *broker*), and other devices can subscribe to those topics and receive messages in real time. There are many cloud-based MQTT servers availale on the Internet.  Two popular ones I have experience with are [CloudMQTT](https://www.cloudmqtt.com/) and [HiveMQ](https://www.hivemq.com/). HiveMQ lets you create a free account, which you can use for personal projects. 

A couple of key things I learned that make Azure IoT Hub different from the above MQTT servers are:
- Unlike CloudMQTT and HiveMQ, Azure IoT Hub is not a generic MQTT broker.  Rather, Azure IoT Hub is a much more powerful system that connects with a variety of other Azure back-end services providing a [plethora of features and integrations](https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/iot).
- Though MQTT is one of the most popular communication protocols in the IoT world, Azure IoT Hub [does not fully support MQTT protocol](https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support). Some of the facts I personally observed were:
   - Unlike other MQTT providers, you cannot use a device/client ID without registering it on the server.
   - Other MQTT providers lets you use custom topic names. In Azure IoT Hub, topic channels use a specific naming convention, and the messages should be sent to the device-specific topic channel.
   - Usually in MQTT protocol, both the publisher and subscriber use the same topic name for communication.  In Azure IoT Hub, they are named differently.  
   - With the very nature of MQTT protocol, CloudMQTT and HiveMQ routes the messages from one device to another automatically as long as both devices are connected to the same topic, making it possible to do direct device-to-device communication.  Azure IoT Hub, on the other hand, does not support automatic device-to-device communication this way.  The pattern is limited to either C2D or D2C. There are ways around this, but needs extra effort.         
- Contrary to other MQTT brokers, a very nice capability Azure IoT Hub has up its sleeve is the synchronous communication through Direct Methods.  While turning on/off an appliance can be implemented through an async publish-subscribe pattern, it will be much simpler to do so in a synchronous manner.  Direct Method helps us achieve the synchronous communication, where our application sends a command to the IoT device to turn on/off the appliance and gets immediate response from the device in the form of a status code and a response payload.  In a way, this justifies the use of Azure IoT Hub in our solution even though ours is a small use case, because we are solving a synchronous problem with a synchronous solution.

Overall, the above findings make me think that Microsoft's focus is more on offering a robust IoT platform with numerous integration endpoints and analytical capabilities rather than providing a generic MQTT broker so that their customers can build large industrial-grade IoT solutions in combination with other Azure services.  For this reason, you may be better off with a generic MQTT broker for simple use cases.  However, trying it out is still an exercice worth doing, because the learning you obtain from the exercise will be valuable in determining a better solution for your next IoT project.

## About Azure App Service Web App
We will host our app as an Azure App Service Web App.  Azure App Service is a [platform as a service (PaaS)](https://en.wikipedia.org/wiki/Platform_as_a_service) offering with [numerous features](https://learn.microsoft.com/en-us/azure/app-service/overview). App Service enables you to host web applications and REST APIs easily and quickly.  Read more about Azure App Service and its features [here](https://learn.microsoft.com/en-us/azure/app-service/overview).  

## About Espressif ESP32
For the IoT device, we will use Espressif ESP32.  ESP32 is a low-cost, low-power microcontroller with Bluetooth and WiFi capabilities as well as many general purpose input/output (GPIO) pins that can be programmed using C++.  For example, you could set a GPIO pin's mode as input and attach a temperature sensor to it, continually read the current room temperature, and send it to a cloud server such as IoT Hub to show it on a dashboard.  On the other hand, you could also program a pin as output and emit a boolean HIGH/LOW voltage to the pin to control an output device.  We use the latter approach in our example to control an [electromagnetic relay](https://randomnerdtutorials.com/guide-for-relay-module-with-arduino/), which in turn controls the appliance.  ESP32 is available in different flavors.  For reference, I use [Freenove ESP32 WROVER CAM module](https://randomnerdtutorials.com/getting-started-freenove-esp32-wrover-cam/), which is available on Amazon.  Read more about different ESP32 modules [here](https://en.wikipedia.org/wiki/ESP32).  

## Implementation
We will start the implementation with the provisioning of Azure IoT Hub. We will then build the ESP32, connect it to Azure IoT Hub, and test it using [Azure IoT Explorer](https://learn.microsoft.com/en-us/azure/iot-fundamentals/howto-use-iot-explorer).  Afterwards, we will build the web application and test the integration locally.  Finally, we will host the web application in Azure App Service and test it end to end to make sure all components works together as expected.  

- ### Provision IoT Hub
   To provision IoT Hub, follow this [Azure IoT Hub provisioning tutorial](https://github.com/nejimonraveendran/AzureIoT/tree/main/cert-based-auth).  
   
   After completing the provisioning tutorial, you have the following:
   
   1. Azure IoT Hub with host name *myorgiothub.azure-devices.net*.
   2. A logical device named *myiotdevice1* in Azure IoT Hub.
   2. A client certificate file named *myiotdevice1.pem* and its private key file *myiotdevice1.key* on the local computer.

- ### Build the Device
   **Wiring Diagram**: Here are the instructions to wire the relay:

   ![image](https://user-images.githubusercontent.com/68135957/224414729-fd67f71c-8020-4f6d-99af-cc5ff53ce0b7.png)

   **Coding**: For the C++ code (aka *sketch*), refer to *Esp32AzureIoT* folder in the source code.  I use [Arduino IDE](https://www.arduino.cc/en/software) for developing the ESP3 code. If you do not have Arduino IDE installed on your Windows computer, you can follow [this tutorial](https://randomnerdtutorials.com/installing-the-esp32-board-in-arduino-ide-windows-instructions/) to set it up for ESP32 development. For Arduino IDE setup specific to ESP32 WROVER module, take a look at [this tutorial](https://randomnerdtutorials.com/getting-started-freenove-esp32-wrover-cam/).
   
   The main highlights of our code are given below:

   First of all, we define a bunch of variables:
   
   ```cpp
   //wifi settings:
   const char* _ssid = "Your_Wifi_Name"; //replace with your 2.4 GHz SSID (network name) 
   const char* _password = "Your_Wifi_Password"; //replace with your wifi password.

   //Azure IoT Hub  MQTT settings
   const char* _mqttServer = "myorgiothub.azure-devices.net"; //IoT Hub host name
   const int _mqttPort = 8883; //IoT Hub Port
   const char* _deviceId = "myiotdevice1"; //device ID configured on IoT Hub
   const char* _mqttUserName = "myorgiothub.azure-devices.net/myiotdevice1/?api-version=2021-04-12"; //format: <hostname>/<device_id>/?api-version=2021-04-12
   const char* _mqttPassword = ""; //no password required because we are using certificate-based auth
   const char* _subDirectMethod = "$iothub/methods/POST/#"; //do not change

   int _relayPin = 13; //the pin to connect the relay (with optocoupler) module
   ```
   
   In the above code, you must set the *_ssid* to the 2.4 GHz channel of your your WiFi.  Nowadays, WiFi routers come with a 5 GHz band as well, and your WiFi router may be exposing an extra network (SSID) at that band, so make sure you are using the correct one.    

   Under MQTT settings, make sure you set the *_mqttServer* host name as displayed on the Azure IoT Hub.  The *_deviceId* also must match the Device ID configured on the Azure IoT Portal. The *_mqttPassword* must be set to an empty string because we are using certificate-based authentication.  The *_relayPin* variable holds the name of the ESP32 GPIO pin that we plan to connect the electromagnetic relay to. I used a normally-open [optocoupler relay](https://robu.in/the-basics-of-optocoupler-relay/), triggered by a LOW pulse.  

   Next, we define 3 variables to store the certificate information:

   ```cpp
      //Azure IoT Hub Root certificate (DigiCert Global Root G2)
      //for TLS transport encryption
      const char* _rootCertAzureIotHub PROGMEM = R"CERT(
         ....
      )CERT";

      //The client/device certificate specific to myiotdevice01
      const char* _clientCert PROGMEM = R"CERT(
         ....
      )CERT";

      //client/device certificate's private key
      const char* _clientPrivateKey PROGMEM = R"CERT(
         ....
      )CERT";

   ```
   We already have the values for *_clientCert* and *_clientPrivateKey* available from the certificate generation step earlier in the article.  Open the *myiotdevice1.pem* file in Notepad and copy its contents to the *_clientCert* variable.  Next, open the private key file *myiotdevice1.key* and copy its contents to *_clientPrivateKey* variable.

   For finding the value for *_rootCertAzureIotHub* variable, follow the steps below:

   Open command prompt (any location), and issue the following command:

   ```bash
   openssl s_client -showcerts -connect myorgiothub.azure-devices.net:8883 > server_cert.crt
   ```
   The above command creates a *server_cert.crt* file in the current path.  Go to the path in Windows Explorer, and double-click to open the file. In the Certificate window that appears, click "Certification Path" tab, select "DigiCert Global Root G2" node, and then click "View Certificate". In the popup window, go to "Details" tab and click "Copy to File". Refer to the screenshot below: 
   ![image](https://user-images.githubusercontent.com/68135957/224374875-2851af84-5c51-4ce4-afe6-c58bf165c0aa.png)

   Follow the "Copy to File" Wizard, select "Base-64 encoded X.509 (.CER)" in the second step, and save it to the same folder where the *server_cert.crt* is, with the name *server_cert.cer*.  Now go to the folder in Windows Explorer, and you should now see a *server_cert.cer* file there.  Open it in Notepad and copy its contents to *_rootCertAzureIotHub* variable. Now we are done with the certificate setup.

   In the *setup* function of the sketch, we the have the code to connect to local WiFi, set the necessary certificates, and finally connect to the Azure IoT Hub through MQTT protocol.  The library we use for MQTT connectivity is [PubSubClient](https://pubsubclient.knolleary.net/api) version 2.8.

   ```cpp
   void setup() {
      pinMode(_relayPin, OUTPUT);
      digitalWrite(_relayPin, HIGH); //I use a relay module triggered by LOW pulse, so keep the relay off through HIGH initially.

      Serial.begin(115200); //initialize serial monitor
      
      connectToWifi(); //connectToWifi function defined in the sketch.

      _wifiClient.setCACert(_rootCertAzureIotHub); //set TLS server certificate
      _wifiClient.setCertificate(_clientCert); // set client certificate for auth
      _wifiClient.setPrivateKey(_clientPrivateKey);	// set client certificate private key

      _mqttClient.setServer(_mqttServer, _mqttPort); //connect to Azure IoT Hub
      _mqttClient.setCallback(messageReceivedHandler); //set callback event listener.
   }
   ```   

   *messageReceivedHandler* is the callback function that handles the messages from IoT Hub.  The highlights of the function are:

   ```cpp
   void messageReceivedHandler(char* topic, byte* payload, unsigned int length) {
      ...
      ...
      String directMethodIdentifier = "$iothub/methods/POST/"; //IoT hub publishes Direct Method messages to this MQTT topic channel
      String ridIdentifier = "/?$rid="; //Request ID format. Each incoming request contains a unique ID. 
      
      //now check if incoming request is really a Direct Method call.
      String topicName(topic);
      if(topicName.indexOf(directMethodIdentifier) < 0 ){ //not a Direct Method call.
         Serial.println("Not a method call");
         return;
      }  

      //parse method name and request ID and construct the response url.
      String methodName = topicName.substring(topicName.indexOf(directMethodIdentifier) + directMethodIdentifier.length(), topicName.lastIndexOf(ridIdentifier));
      String requestId = topicName.substring(topicName.indexOf(ridIdentifier) + ridIdentifier.length());
      String responseUrl = "$iothub/methods/res/200" + ridIdentifier + requestId; //format accepted by IoT Hub
      
      if(methodName == "toggle"){ //"toggle" method call received
         digitalWrite(_relayPin, !digitalRead(_relayPin)); //toggle the pin (appliance) state.
      }else if (methodName == "status"){ //"status" method call received. 
         //do something
         //Currently, we don't do anything special.
      }else{
         Serial.println("Invalid method.");
         return;
      }

      //for both toggle and status methods, return the status.   
      int status = !digitalRead(_relayPin);
      String responsePayload = "{\"Status\":" + String(status) + "}";

      Serial.println("Response [Url: " + responseUrl + ", Payload: " + responsePayload + "]");

      //publish response back to IoT Hub
      publishMessage(responseUrl.c_str(), responsePayload.c_str()); //method defined in the sketch (check source code)
   
   }
   ```   
   In the above code, *toggle* and *status* are the Direct Methods we have defined.

   Note: It is very important that we use Azure-defined topic names and response URLs for Direct Method invocation when using MQTT protocol. Refer to [this documentation](https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods) for details. 

   Finally, in the *loop* function of the sketch, we have:

   ```cpp
   void loop() {
      if(!_mqttClient.connected()){
         connectToMqttServer();
      }

      _mqttClient.loop();
   }
   ```
   All what it does is make sure the MQTT client is always connected to Azure IoT Hub and subscribing to messages. Inside the *connectToMqttServer* metod, the following line of code subscribes to Azure IoT Hub Direct Methods:

   ```cpp
   _mqttClient.subscribe(_subDirectMethod); //subscribe to all Cloud2Device Direct Method calls.
   ```
   
   **Testing the Device**: At this point, you can test the device without needing to build the web application.  For this, you will need to download and install [Azure IoT Explorer](https://github.com/Azure/azure-iot-explorer/releases).  In Azure IoT Explorer, add a new connection using the connection string method. The connection string can be obtained from your Azure IoT Hub resource page through the menu "Security Settings" -> "Shared Access Policy" -> iothubowner ->  Primary connection string.  Refer to the screenshot below:
   ![image](https://user-images.githubusercontent.com/68135957/223919719-3c139208-1c27-4ea5-9a63-c5ea37e33f20.png)
   
   
   Once the connection is added, you can invoke methods (*toggle* and *status*) defined in the device's code, as shown below: 
   ![image](https://user-images.githubusercontent.com/68135957/224435239-a9530bdd-8238-4bea-aea4-c83bfe9fb05c.png)

   When you click "Invoke method", your device should toggle its on/off state now!

- ### Build Web Application
   **Coding:**  The web application is an ASP.NET Core MVC application targeting .NET 6 (refer to *AzureIoTApp* folder in the source code).  The most interesting piece of code is the ApplianceService class.  This class acts as a Service that connects to Azure IoT Hub.  For service communication, we use the official [Azure IoT Hub service SDK](https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-sdks), which is available as nuget package through the following command:

   ```powershell
   NuGet\Install-Package Microsoft.Azure.Devices -Version 1.38.2
   ```

   There are mainly 2 methods in the *ApplianceService* class.  The following method is intended to query the on/off status of the device (invokes the *status* method defined in the device):

   ```cpp
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
   ```
   
   The method below is to toggle on/off state of the appliance (invokes the *toggle* method defined in the device):

   ```cpp
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
   ```
   
   Both of the above methods use AMQP protocol to establish service-to-hub connection with Azure IoT Hub.  Azure IoT Hub in turn invokes the Direct Methods in the device using MQTT protocol, because MQTT is the protocol we use for the connection between the device and the IoT Hub. As mentioned above, *"status"* and *"toggle"* are the names of the Direct Methods defined in the device code. Refer to the "Build the Device" section for more details about these methods.  The *_targetDeviceId* variable holds the device name as defined on Azure IoT Hub (in our example *myiotdevice1*).  The value for *_connectionString* is also obtained from Azure IoT Hub, as shown in the screenshot below:
   ![image](https://user-images.githubusercontent.com/68135957/223919719-3c139208-1c27-4ea5-9a63-c5ea37e33f20.png)


   **Authentication Credentials:**  Our application is protected by login credentials (username + password).  For the sake of simplicity, we will keep the credentials in the *appsettings.json* file.  Instead of storing the password in plain text, we will use a password hash/salt combination. If you want to add a new user, you will need to generate the password hash and store them in the *appsettings.json* under the *UserStore:Users* node.  For generating password hash, you can use the Hash helper page in the application, which can be accessed at */Home/Hash* path.  

   At this point, you should be able to run the web application locally and test the solution end to end.

- ### Host in Azure App Service  
   Now that we are done with the building and testing locally, it is finally time to host the web application in Azure App Service. To provision an App Service instance, follow [this tutorial](https://github.com/nejimonraveendran/AzureIoT/tree/main/azure-app-service).

   If you have followed the exact same tutorial above, you now have a web site running at [applianceapp.azurewebsites.net](https://applianceapp.azurewebsites.net), waiting to receive the content. 

   In Visual Studio Solution Explorer, right click the web application project, click Publish, and follow the wizard instructions to publish the web application to Azure.  Make sure to select Azure App Service (Windows) when prompted for the platform OS.

   When the publish operation is complete, Visual Studio should automatically open the URL to the application in the browser. Try one more time to make sure your cloud application is able to communicate with your IoT device on prem.  

## Conclusion
It was a great experience working on this little project.  I thoroughly enjoyed every step of it. I also learned several things about Azure IoT Hub, ESP32, and MQTT protocol.  I am aware that smart switches are available in the market and it is not something someone needs to reinvent.  However, building something as a means to learn something new is not a bad idea. And from that perspective, it was time well spent!







  
