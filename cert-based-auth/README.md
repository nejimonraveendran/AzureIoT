# How to create an IoT device in Azure IoT Hub with certificate-based authentication
Azure IoT Hub supports 2 different types of device authentication:
- **Symmetric Key authentication:**  In this method, an IoT device will have a pre-created symmetric key on Azure IoT hub.  At the time of the authentication, the device will present a shared access signature based on the key as the proof of identity. 
- **Certificate-based authentication:**  This is the method we are discussing in this document.

The certificate-based authentication works like this: There will be a root certificate signed by a certificate authority.  The root certificate will be uploaded to the Azure IoT Hub. Then, for each IoT device we want to authenticate, we will create a separate client certificate, signed by the same certificate authority.  At the time of the authentication, our IoT device will present the client certificate as the proof of identity. Azure IoT Hub will verify the identity based on the root certificate and the device name.  This document details how to create a certificate authority, a root certificate, and the client certificate(s). 

**Important Note:** The certificate generation method uses our own custom certificate authority, which is recommended for testing purposes only.  For production scenarios, use a proper public certificate authority (eg. Entrust).

This document is based on the official Microsoft documentation, but it uses a simplified approach involving fewer steps, since we are doing it for testing.  The link to the documentation is here for reference: https://learn.microsoft.com/en-us/azure/iot-hub/tutorial-x509-openssl.  This tutorial assumes that the operations are carried out on a Windows machine.

### 1. Create Root Certificate
In this step, we will create a certificate authority and a root certificate signed by the certificate authority.
- **Install OpenSSL:** If OpenSSL is not already installed, get **OpenSSL for Windows** from [here](https://wiki.openssl.org/index.php/Binaries).  Add the OpenSSL bin path to Windows PATH environment variables.
 
- **Create root CA directory structure:** Open command prompt (CMD) in C drive as Administrator, and issue following commands: 
  ```bash
    mkdir IoTCerts
    cd IoTCerts
    mkdir rootca
    cd rootca
    mkdir certs db private
    type nul > db/index
    openssl rand -hex 16 > db/serial
    echo 1001 > db/crlnumber
  ```
  The command prompt looks like the following:

  ![image](https://user-images.githubusercontent.com/68135957/222215950-bfbe9c31-ef65-429e-8814-d42110f2ba98.png)

- **Create root.ca config file:** Open Notepad and paste the following content.  Save it to the *rootca* folder with the file name *rootca.conf*. This file will work as the base configuration for all the certificates we are going to generate, so you might want to change certain values in the configuration.  For example, you might want to use your own *domain_suffix* and *commonName* as per your liking.  The *default_days* sets the default expiration period of the certificates we are going to generate. Current default is 10 years (3650 days).    

  ```
  [default]
  name                     = rootca
  domain_suffix            = myorg.com
  aia_url                  = http://$name.$domain_suffix/$name.crt
  crl_url                  = http://$name.$domain_suffix/$name.crl
  default_ca               = ca_default
  name_opt                 = utf8,esc_ctrl,multiline,lname,align

  [ca_dn]
  commonName               = "MyOrg Root CA"

  [ca_default]
  home                     = ../rootca
  database                 = $home/db/index
  serial                   = $home/db/serial
  crlnumber                = $home/db/crlnumber
  certificate              = $home/$name.crt
  private_key              = $home/private/$name.key
  RANDFILE                 = $home/private/random
  new_certs_dir            = $home/certs
  unique_subject           = no
  copy_extensions          = none
  default_days             = 3650
  default_crl_days         = 365
  default_md               = sha256
  policy                   = policy_c_o_match

  [policy_c_o_match]
  countryName              = optional
  stateOrProvinceName      = optional
  organizationName         = optional
  organizationalUnitName   = optional
  commonName               = supplied
  emailAddress             = optional

  [req]
  default_bits             = 2048
  encrypt_key              = yes
  default_md               = sha256
  utf8                     = yes
  string_mask              = utf8only
  prompt                   = no
  distinguished_name       = ca_dn
  req_extensions           = ca_ext

  [ca_ext]
  basicConstraints         = critical,CA:true
  keyUsage                 = critical,keyCertSign,cRLSign
  subjectKeyIdentifier     = hash

  [sub_ca_ext]
  authorityKeyIdentifier   = keyid:always
  basicConstraints         = critical,CA:true,pathlen:0
  extendedKeyUsage         = clientAuth,serverAuth
  keyUsage                 = critical,keyCertSign,cRLSign
  subjectKeyIdentifier     = hash

  [client_ext]
  authorityKeyIdentifier   = keyid:always
  basicConstraints         = critical,CA:false
  extendedKeyUsage         = clientAuth
  keyUsage                 = critical,digitalSignature
  subjectKeyIdentifier     = hash
  ```

- **Create a root certificate authority (CA)**:  Next, we need to create our own certificate authority, who will sign all our certificates. As mentioned above, this is for testing only. For production scenarios, a public CA should be used. There are 2 steps involved here. From the command prompt (under the *rootca* folder), issue following commands (when prompted for a passphrase, enter a custom string and note it down.  You will need to re-enter it in later steps):
  - Generate a certificate signing request and a private key (command): ``` openssl req -new -config rootca.conf -out rootca.csr -keyout private/rootca.key ```  
  - Generate a self-signed root certificate from the root CSR (command): ``` openssl ca -selfsign -config rootca.conf -in rootca.csr -out rootca.crt -extensions ca_ext ``` 
- **Create PEM certificate out of CRT certificate:** In Azure, we need to upload the root certificate in PEM format, so convert the CRT format to PEM using the command: ``` openssl x509 -in rootca.crt -out rootca.pem -outform PEM ```.  
  
  We will use the PEM certificate later in the article when we go to Azure Portal to create the logical IoT device.  After the above operations, the command prompt looks somewhat like the following:
  ![image](https://user-images.githubusercontent.com/68135957/222210272-4a194d0b-a67c-4c35-a2ee-ff047f7d7ae5.png)

  In Windows Explorer, now you can see a *rootca.crt* created in the *rootca* folder:
  ![image](https://user-images.githubusercontent.com/68135957/221923082-c48ef4fd-a68a-46e4-a6b9-6d9859acd488.png)

Now that we have the root certificate, we are ready to generate the client (device) certificate.

### 2. Create Client (Device) Certificate
A client certificate is device-specific, proving the device's identity. For this reason, the client certificate is tied to the IoT device's name.  What this means is that the device name used on the Azure IoT portal and the common name used for creating the client certificate should match.  At the time of authentication, IoT Hub will verify factors such as the client certificate's signing authority against the root certificate authority as well as the client certificate's common name against the device name configured on the Azure IoT portal.  In this example, we will create a certificate for a device named *myiotdevice1*:
- **Create client (Device) certificates directory structure:** While you are at the *rootca* folder on the command prompt, issue following commands:
  ```bash
    cd..
    mkdir devices
    cd devices
    mkdir db
  ```
- **Generate client certificate for myiotdevice1:** 
- The following commands (within the *devices* folder) create a private key, a CSR, and finally the actual certificate in CRT and PEM formats. When building applications for devices like ESP32 microcontroller, we will use the PEM certificate. Optionally, you might also want to generate a certificate in PFX format out of the CRT format to use it on certain other devices/desktop applications to simulate a physical IoT device.  When prompted for the Common Name/FQDN, enter *myiotdevice1*: 
  ```bash     
    openssl rand -hex 16 > db/serial
    openssl genpkey -out myiotdevice1.key -algorithm RSA -pkeyopt rsa_keygen_bits:2048 
    openssl req -new -key myiotdevice1.key -out myiotdevice1.csr
    openssl ca -config ..\rootca\rootca.conf -in myiotdevice1.csr -out myiotdevice1.crt -extensions client_ext
    openssl x509 -in myiotdevice1.crt -out myiotdevice1.pem -outform PEM
    openssl pkcs12 -export -in myiotdevice1.crt -inkey myiotdevice1.key -out myiotdevice1.pfx
  ```
  
  Note: If you want to generate more than one client certificates, e.g., for another device named *myiotdevice2*, repeat the above 6 commands with the correct device name.
  
  Now the command prompt screenshot looks somewhat like this:
  ![image](https://user-images.githubusercontent.com/68135957/222236802-db37afa2-b4ff-43bb-9686-df60f5114cc3.png)

### 3. Create Azure IoT Hub and Import Server Certificate 
In this section, we are going to log in to Azure Portal and create an IoT Hub resource and add 1 logical IoT device named *myiotdevice1*.  We will set the authentication type as *Root CA-signed certificate-based*.  Then, we will import the root certificate *rootca.pem* we generated in the *rootca* folder.
- **Create Azure IoT Hub:**
  - Log in to http://portal.azure.com, search for "IoT Hub" in the search box, and click Create (+) icon, and create an IoT Hub resource.
   ![image](https://user-images.githubusercontent.com/68135957/222238822-3cefb43d-fda1-47fe-a05d-85633cc19f4b.png)
   
  - Under "Networking", grant Public Access:
    ![image](https://user-images.githubusercontent.com/68135957/221958811-94a5769b-a7c5-4fab-a9f6-0145abdbcd06.png)

  - Under "Management", set permission level as "Shared access policy + RBAC":
    ![image](https://user-images.githubusercontent.com/68135957/221959016-26de225c-d9d5-4812-acb0-b7408c12b165.png)
 
  - Review and Create.  Once done, click "Go to resource" button to go to the resource Overview page. Here is the screenshot for reference: 
  ![image](https://user-images.githubusercontent.com/68135957/224428910-63738d23-38d9-4839-81e2-abf865098582.png)

  
    On the resource Overview Page, make a note of the *Hostname* field (in this example: *myorgiothub.azure-devices.net*).  We will need this info to connect our device to the hub, as discussed in [this hands-on tutorial](https://github.com/nejimonraveendran/AzureIoT/blob/main/README.md).

    Note: At the time of writing this, the Overview page displays a message "Baltimore CyberTrust certificate will expire in 2025".  Since ours is a new implementation, click "What do I need to do" link, then click "Migrate to DigiCert Global G2" button, and follow the instructions to update the certificate.
 
 - **Import Root Certificate**
    - On the IoT Hub resource Overview page, click "Certificates" menu on the left blade, and click the "Add" button.
      ![image](https://user-images.githubusercontent.com/68135957/222241701-618d40b8-80fd-4927-a563-ec5bcd510396.png)

    - In the "Certificates" blade that appears, give a certficate name (eg. MyOrgIoTHubRootCertificate).  Browse to the *IotCerts/rootca* folder on your computer and import the *rootca.pem*.  Check the "Set certificate status to verified on upload" checkbox.  
      ![image](https://user-images.githubusercontent.com/68135957/222242138-652c11e9-6f60-47e2-a46a-4274ea8bf6df.png)      

    - Click Save.
      
- **Create Logical Device myiotdevice1**
    - On the resource Overview page, click "Devices" menu on the left blade, and click "Add Device" button.
      ![image](https://user-images.githubusercontent.com/68135957/222243115-27b3b538-0036-4940-9df2-7d82566e24d9.png)  
    - In the "Create a device" page that appears, give *myiotdevice1* as the Device Id.  Note that this name must match the commonName/FQDN we used when generating the client (device) certificate above. Select Authentication type *X.509 CA Signed*.  Keep "Connect this device to an IoT Hub" as *Enabled*.
      
      ![image](https://user-images.githubusercontent.com/68135957/222243887-1653c70b-772c-4661-a3de-57b834c7128b.png)
    
    - Click Save.
      
Now we have completed all the steps required for generating the certificates and creating the devices on Azure Portal!  To learn how to build an IoT device and use the client certificates when connecting to Azure IoT Hub, take a look at this project: [Build a Smart Switch Using Azure IoT Hub, Espressif ESP32, and ASP.NET Core](https://github.com/nejimonraveendran/AzureIoT)  

