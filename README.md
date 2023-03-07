# Home Automation Using Azure IoT Hub, Espressif ESP32 Microcontroller, and ASP.NET Core

Internet of Things (IoT) and Azure are 2 areas I love to play around. The purpose of this article is to document the hands-on experience and learnings obtained from implementing a cloud- and IoT-based home automation project.  The technologies used are Azure IoT Hub, ASP.NET Core, and ESP32 microcontroller. If you follow this tutorial, you will basically build a solution to securely control (on/off) any electric appliance (e.g., a home light) remotely through your smartphone or computer. 


## The Problem
**The requirement/user Story:**  As a the owner of the appliance, I, the user, want to turn on/off a home appliance remotely through my phone/laptop with the following conditions.  
1. I should be able to control the appliance even while I am away from my home.
2. The communication between my application and the device should be secure, i.e., encrypted with TLS 1.2.
3. Access to the ON/OFF button page should be controlled by proper authentication and authorization.  Currently, only I should be able to log in.
4. If I decide to build it as a native mobile app, I should be able to do so without making huge changes to the web application.  

## The Solution
The architecture of the solution looks like the following:
![image](https://user-images.githubusercontent.com/68135957/223001314-045b2ff0-0edc-40b1-9ab3-202e3b8e67f9.png)

There are 3 main modules in the solution:
1. **Azure IoT Hub:**  The IoT messaging hub provisioned in Azure.   
2. **ASP.NET MVC Web Application:**  The user-facing web application, publishing "on/off" messages to the Azure IoT Hub.
3. **Espressif ESP32 Micro-controller:** The physical IoT device at home, subscribing to "on/off" messages from Azure IoT Hub.

**The solution looks and works like this:** I will build an ASP.NET Core MVC Application, which will be hosted as an [Azure App Service Web App](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore?tabs=net60&pivots=development-environment-vs) with a public URL so that I can access it from my personal devices even when I am away from home.  The application's home page will show an ON/OFF button.  When I click the button, the web application will publish an "on/off" message to the Azure IoT Hub. On the other side, an [Espressif ESP32](https://en.wikipedia.org/wiki/ESP32) microcontroller at home has already established a connection to the IoT Hub and beeen subscribing to messages from the hub.  Upon reception of the message from the web application, the IoT Hub will send it to the device, and the device will act accordingly, i.e., turn on or off the appliance through an electromagnetic relay.  In addition, the device will report the current status of the light back to the web application through the IoT Hub so that I, the user, will get immediate feedback about the success/failure of the action.        

## About Azure IoT Hub
[Azure IoT Hub](https://learn.microsoft.com/en-us/azure/iot-hub/iot-concepts-and-iot-hub) is a fully managed PaaS solution that functions as a messaging hub between applications and physical IoT devices. There are several messaging patterns that the IoT Hub supports.  They are mainly:
1. **Device to Cloud (D2C) messaging:**  In this pattern, an IoT device asynchronously sends messages to IoT Hub.  This is also known as Telemetry.
2. **Cloud to Device (C2D) messaging:**  A cloud application asynchronously sends messages back to the physical device.
3. **Direct Method calls:**  This is also a cloud-to-device message but done in a synchronous manner.

Since the nature of the #1 and #2 above are asynchronous, they are more suitable for use cases where we just fire a message and forget, i.e., we do not need to wait for an immediate response.  In our use case, it is desired to get an immediate response if the appliance was turned on/off.  For this reason, the best pattern to use is Direct Method.

Azure IoT Hub supports several communication protocols as well (HTTPS, AMQP, WebSockets, MQTT, etc.).  In any IoT-based solution, probably the most widely used communication protocol is [MQTT](https://en.wikipedia.org/wiki/MQTT). MQTT is a publish-subscribe pattern, where devices can publish messages as "topics" to an MQTT server (aka broker) and other devices can subscribe to those topics and receive messages in real-time. There are many cloud-based MQTT servers availale on the Internet  such as [CloudMQTT](https://www.cloudmqtt.com/), [HiveMQTT](https://www.hivemq.com/), etc. HiveMQ lets you create a free account, which you can use for personal projects. 

A few key things I learned that make Azure IoT Hub and other popular cloud-based MQTT servers distinguish from each other are:
1. Unlike CloudMQTT, HiveMQTT, etc., Azure IoT Hub is not a generic MQTT broker.  Rather, Azure IoT Hub is a much more powerful system that connects with a variety of other Azure back-end services providing a [plethora of features and integrations](https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/iot).
2. Though MQTT is one of the most popular communication protocols in the IoT world, Azure [does not fully support MQTT protocol](https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support). Some of the facts I personally observed were:
   - Unlike other MQTT providers, you cannot use a Device/Client ID without registering it on the server.
   - Other MQTT providers lets you use custom topic names. In Azure IoT Hub, topic channels use a specific naming convention, and the messages should be sent to the device-specific topic channel.  
   - Usually in MQTT protocol, both the publisher and subscriber uses the same topic name for communication.  In Azure IoT Hub, they are named differently.  
     
   It makes me think that Microsoft's focus is more on offering a robust IoT platform with numerous integration endpoints and analytical capabilities rather than providing a generic MQTT broker so that their customers can build large industrial-grade IoT solutions in combination with other Azure services.  For this reason, you may be better off with a generic MQTT broker for simple use cases such as turnig on/off an appliance.  However, it is still an exercice worth doing because the learning you obtain from the exercise will be valuable in determining a solution for your next IoT project.     
3. Unlike other MQTT brokers, very nice capability Azure IoT Hub has up its sleeve is the synchronous communication through Direct Methods.  While turning on/off an appliance can be implemented through async publish-subscribe pattern, it will be much simpler to do so if it can be done in a synchronous manner.  Direct Method helps us achieve the synchronous communication, where our application sends a command to the IoT device to turn on/off the appliance and gets immediate response from the device about the success/failure of the action.  In a way, this justifies the use of Azure IoT Hub in our simple use case, because we are solving a synchronous problem with a synchronous solution. 

## About Azure App Service Web App
We will host our app as an Azure App Service Web App.  Azure App Service is a [platform as a service (PaaS)](https://en.wikipedia.org/wiki/Platform_as_a_service) offering with numerous features that enables you to host web applications and REST APIs easily and quickly.  Why don't we use containerization?  For a use case like this, I think App Service is a perfect middleground between hosting web applications on virtual machines (VMs) and containerization technologies such as Docker/Kubernetes, because you do not have to manage a VM infrastructure and at the same time do not have to deal with the complexities of containerization. Read more about Azure App Service and its features [here](https://learn.microsoft.com/en-us/azure/app-service/overview).  

## About Espressif ESP32
For the IoT device, we will use Espressif ESP32.  ESP32 is a low-cost, low-power microcontroller with Bluetooth and WiFi capabilities as well as many general purpose input/output (GPIO) pins that can be programmed using C++.  For example, you could set a GPIO pin's mode as input and attach a temperature sensor to it, which will read the current room temperature and send it to a cloud server such as IoT Hub.  On the other hand, you could also program a pin as output and emit a boolean HIGH/LOW voltage to the pin to control an output device.  We use the latter approach in our example to control an electromagnetic relay, which in turn control the appliance.  ESP32 is available in different flavors.  For reference, I use an ESP32-CAM module, which is available on Amazon.  Read more about different ESP32 modules [here](https://en.wikipedia.org/wiki/ESP32).  

## Implementation
Since we have 3 modules, our implementation will also be split into 3 parts. We will start with the provisioning of Azure IoT Hub, then build and host the web application, and finally build the ESP32 part and test all of them together.  

 ### Provision IoT Hub
 To provision IoT Hub, follow this [Azure IoT Hub provisioning tutorial](https://github.com/nejimonraveendran/AzureIoT/tree/main/cert-based-auth).  
 
 If you have followed the provisioning tutorial, you now have the following:
 
 1. A logical device in IoT Hub named *myiotdevice1*.
 2. A client certificate named *myiotdevice1.crt* and its private key file named *myiotdevice1.key* on your local computer.

 ### Build Web Application
 instructions to build web app
 Refer to this article to learn how to create an Azure App Service Web App 
 
 #### Testing without actual device

 ### Build the Device
 #### Wiring Diagram
 instructions to wire the relay

 #### Coding
instructions to build esp code

## Conclusion
Overall implementation notes 






  
