# How to create an IoT device in Azure IoT Hub with certificate-based authentication
Azure IoT Hub supports 2 different types of device authentication:
- Symmetric Key authentication:  In this method, our IoT device will have a pre-created symmetric key on Azure IoT hub.  At the time of the authentication, the device will present a shared access signature based on the key as the proof of identity. 
- Certificate-based authentication:  This is the method we are discussing in this document.

The certificate-based authentication works like this: There will be a root certificate signed by a certificate authority.  The root certificate will be uploaded to the Azure IoT Hub. We will then create one client certificate, signed by the same certificate authority, for each IoT device we want to authenticate.  At the time of the authentication, our device will present the client certificate as the proof of identity. This document details how to create a certificate authority, root certificate, and the client certificate(s). 

Important Note: The certificate generation method uses our own custom certificate signing authority, which is recommended for testing purposes only.  For production scenarios, use a proper public certificate authority (eg. Entrust). This tutorial assumes that the operations are carried out on a Windows machine.

This document is based on the official Microsoft documentation, but it uses a simplified approach involving fewer steps, since we are doing it for testing.  The link to the documentation is here for reference: https://learn.microsoft.com/en-us/azure/iot-hub/tutorial-x509-openssl

### 1. Install OpenSSL
If OpenSSL is not already installed, get **OpenSSL for Windows** from [here](https://wiki.openssl.org/index.php/Binaries).  Add the OpenSSL bin path to Windows PATH environment variables.

### 2. Create a Root Certificate to be used on Server (Azure IoT Hub)
Since we plan to use our own certificate authority, the first task is to create a certificate authority.  Then, we will generate a server certificate signed by this certificate authority.
- **Create root CA directory structure:** Open command prompt (CMD) in C drive as Adminstrator, and issue following commands: 
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
  The window looks like the following:

  ![image](https://user-images.githubusercontent.com/68135957/222215950-bfbe9c31-ef65-429e-8814-d42110f2ba98.png)

- **Create root.ca config file:** Open the *rootca* folder in Windows Explorer and create a file with the name *rootca.conf*.  Open that file in Notepad and paste the following content. This file will work as the base configuration for all the certificates we are going to generate, so change the values in the configuration according to your liking.  For example, you might want to use your own *domain_suffix* and *commonName* as per your liking.  The *default_days* sets the default expiration period of the certificates we are going to generate. Current default is 10 years (3650 days).    

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

- **Create a root Certificate Authority (CA)**:  Next, we need to create our own certificate authority, who will sign all our certificates. As mentioned above, this is for testing only. For production scenarios, a public CA should be used. There are 2 steps involved here. From the command prompt (under the *rootca* folder), issue following commands (when prompted for a passphrase, enter a custom string and note it down.  You will need to re-enter it later):
  - Generate a certificate signing request and a private key (command): ``` openssl req -new -config rootca.conf -out rootca.csr -keyout private/rootca.key ```  
  - Generate a self-signed root certificate from the root CSR (command): ``` openssl ca -selfsign -config rootca.conf -in rootca.csr -out rootca.crt -extensions ca_ext ``` 
- **Create PEM certificate out of CRT certificate:** In Azure, we need to upload the server certificate in PEM format, so convert the CRT format to PEM using the command: ``` openssl x509 -in rootca.crt -out rootca.pem -outform PEM ```.  
  
  We will use the PEM certificate later in the article when we go to Azure Portal to create the logical IoT device.  After the above operations, the CMD window looks somewhat like the following:
  ![image](https://user-images.githubusercontent.com/68135957/222210272-4a194d0b-a67c-4c35-a2ee-ff047f7d7ae5.png)

  In Windows Explorer, now you can see a *rootca.crt* created in the *rootca* folder:
  ![image](https://user-images.githubusercontent.com/68135957/221923082-c48ef4fd-a68a-46e4-a6b9-6d9859acd488.png)

Now that we have the root certificate, we are ready to generate the client (device) certificate.

### 3. Create Client (IoT Device) Certificate
Client certificates are device-specific. A client certificate is tied to a device name, which should match the logical device name configured on the Azure IoT Hub. We will generate a certificate for a device named *myiotdevice1*:
- **Create Client (Device) Certificates Directory Structure:** While you are at the *rootca* folder in the command prompt, issue following commands:
  ```bash
    cd..
    mkdir devices
    cd devices
    mkdir db
  ```
- **Generate client certificate for myiotdevice1:** 
- The following commands (within the *devices* folder) create a private key, a CSR, and finally the actual certificate in CRT format. Optionally, you might also want to generate a certificate in PFX format out of the CRT format to use it on certain devices/desktop applications to simulate a physical IoT device.  When prompted for the Common Name/FQDN, enter *myiotdevice1*: 
  ```bash     
    openssl rand -hex 16 > db/serial
    openssl genpkey -out myiotdevice1.key -algorithm RSA -pkeyopt rsa_keygen_bits:2048 
    openssl req -new -key myiotdevice1.key -out myiotdevice1.csr
    openssl ca -config ..\rootca\rootca.conf -in myiotdevice1.csr -out myiotdevice1.crt -extensions client_ext
    openssl pkcs12 -export -in myiotdevice1.crt -inkey myiotdevice1.key -out myiotdevice1.pfx
  ```
  
  Note: If you want to generate more than one client certificates, for example for another device named *myiotdevice2*, repeat the above 5 commands with the correct device name.
  
  Now the CMD window screenshot looks somewhat like this:
  ![image](https://user-images.githubusercontent.com/68135957/222236802-db37afa2-b4ff-43bb-9686-df60f5114cc3.png)

### 4. Create Azure IoT Hub and Import Server Certificate 
In this section, we are going to log in to Azure Portal and create an IoT Hub resource and add 1 virtual device named *myiotdevice1*.  We will set the authentication type as *Root CA-signed certificate-based*.  Then, we will import the root certificate *rootca.pem* we generated in the *rootca* folder.
- **Create Azure IoT Hub:**
  - Log in to http://portal.azure.com, search for "IoT Hub" in the search box, and click Create (+) icon, and create an IoT Hub resource.
   ![image](https://user-images.githubusercontent.com/68135957/222238822-3cefb43d-fda1-47fe-a05d-85633cc19f4b.png)
   
  - Under Networking, grant Public Access:
    ![image](https://user-images.githubusercontent.com/68135957/221958811-94a5769b-a7c5-4fab-a9f6-0145abdbcd06.png)

  - Under Management, set permission leval as "Shared access policy + RBAC":
    ![image](https://user-images.githubusercontent.com/68135957/221959016-26de225c-d9d5-4812-acb0-b7408c12b165.png)
 
  - Review and create.  Once done, click "Go to resource" button to go to the resource Overview page.  On the resource Overview Page, make a note of the *Hostname* field (in this example: *myorgiothub.azure-devices.net*).  We will need this info to connect our device to the hub.
    
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
    - In the "Create a device" page that appears, give *myiotdevice1* as the Device Id.  Note that this name must match the commonname/FQDN we used when generating the client (device) certificate above. Select Authentication type *X.509 CA Signed*.  Keep "Connect this device to an IoT Hub" as *Enabled*.
      
      ![image](https://user-images.githubusercontent.com/68135957/222243887-1653c70b-772c-4661-a3de-57b834c7128b.png)
    
    - Click Save.
      
Now we have completed all the steps required for generating the certificates and creating the devices on Azure Portal!

