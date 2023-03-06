# Home Automation Using Azure IoT Hub, Espressif ESP32 Microcontroller, and ASP.NET Core

Internet of Things (IoT) and Azure are 2 areas I love to play around. The purpose of this article is to document the hands-on experience and learnings obtained from implementing a cloud- and IoT-based home automation project.  The technologies used are Azure IoT Hub, ASP.NET Core, and ESP32 microcontroller. If you follow this tutorial, you will basically build a solution to securely control (on/off) any electric appliance (e.g., a home light) remotely through your smartphone or computer. 

There are 3 main modules in the solution:
1. **ASP.NET MVC Web Application:**  The user-facing UI application that contains an on/off button.
2. **Azure IoT Hub:**  The IoT messaging hub provisioned in Azure.   
3. **Espressif ESP32 Micro-controller:** The physical IoT device that sits at home/on premises and connected to an electromagnetic relay, which can control any electric appliance.

## How It Works
The architecture of the solution looks like the following:
![image](https://user-images.githubusercontent.com/68135957/223001314-045b2ff0-0edc-40b1-9ab3-202e3b8e67f9.png)

[Azure IoT Hub](https://learn.microsoft.com/en-us/azure/iot-hub/iot-concepts-and-iot-hub) is a fully managed PaaS solution that functions as a messaging hub between applications and physical IoT devices. There are several messaging patterns that the IoT Hub supports.  They are mainly:
1. **Device to Cloud (D2C) messaging:**  In this pattern, an IoT device asynchronously sends messages to IoT Hub.  This is also known as Telemetry.
2. **Cloud to Device (C2D) messaging:**  A cloud application asynchronously sends messages back to the physical device.
3. **Direct Method calls:**  This is also a cloud-to-device message but done in a synchronous manner.

Since the nature of the #1 and #2 above are asynchronous, they are more suitable for use cases where we just fire a message and forget, i.e., we do not need to wait for an immediate response.  In our use case, it is desired to get an immediate response if the appliance was turned on/off.  For this reason, the best pattern to use is Direct Method.

Azure IoT Hub supports several communication protocols as well (HTTPS, AMQP, WebSockets, MQTT, etc.).  In any IoT-based solution, probably the most widely used communication protocol is [MQTT](https://en.wikipedia.org/wiki/MQTT).    
