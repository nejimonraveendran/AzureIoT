# How to create server and client certificates for certificate-based device authentication in Azure IoT
Azure IoT Hub supports 2 different types of device authentication:
- Certificate-based authentication
- Symmetric Key authentication

For certificate-based authentication, we need to create a pair certificates: A server certificate to be uploaded to the Azure Portal when we create logical IoT devices on the portal, and a client certificate to be kept on the physical/simulated IoT device and to be sent to client at the time of connection.

This document details how to create a certificate pair using OpenSSL. 

Important Note: The certificate generation method uses our own custom certificate signing authority, which is recommended for testing purpose only.  For production scenarios, use a proper public certificate authority (eg. Entrust). This tutorial assumes that these operations are carried out on a Windows machine.

This document is based on the official Microsoft documentation, but uses a simplified approach involving fewer steps, since we are doing it for testing.  The link is here for reference: https://learn.microsoft.com/en-us/azure/iot-hub/tutorial-x509-openssl

### 1. Install OpenSSL
If OpenSSL is not already installed, get **OpenSSL for Windows** from [here](https://wiki.openssl.org/index.php/Binaries).  Add the OpenSSL bin path it to Windows PATH environment variable.

### 2. Create a Root Certificate to be used on Server (IoT Hub)
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

- **Create root.ca config file:** Open the *rootca* folder in Windows Explorer and create a file with the name *rootca.conf*.  Open that file in Notepad and paste the following content. This configuration will work as the base for all the certificates we are going to generate, so change the values in the configuration according to your liking.  For example, you might want to use a different *domain_suffix* and *commonName* as per your liking.  The *default_days* sets the default expiration period of the certificates we are going to generate. Default is 10 years (3650 days).    

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

- **Create a root Certificate Authority (CA)**:  Next, we need to create our own certificate authority, who will sign all our certificates. As mentioned above, this is for testing only. For production scenarios, a public CA should be used. There are 2 steps involved here. From the CMD prompt (under the *rootca* folder), issue following commands (when prompted for a passphrase, enter a custom string and note it down):
  - Generate a certificate signing request and a private key: ``` openssl req -new -config rootca.conf -out rootca.csr -keyout private/rootca.key ```  
  - Generate a self-signed root certificate from the root CSR: ``` openssl ca -selfsign -config rootca.conf -in rootca.csr -out rootca.crt -extensions ca_ext ``` 
- **Create PEM certificate out of CRT certificate:** In Azure, we need to upload a certificate in PEM format, so convert the CRT format to PEM using the command: ``` openssl x509 -in rootca.crt -out rootca.pem -outform PEM ```.  
  
  We will use the PEM certificate later in the article when we go to Azure Portal to create the logical IoT device.  After the above operations, the CMD window looks somewhat like the following:
  ![image](https://user-images.githubusercontent.com/68135957/222210272-4a194d0b-a67c-4c35-a2ee-ff047f7d7ae5.png)

  Now you can see a *rootca.crt* created in the *rootca* folder:
  ![image](https://user-images.githubusercontent.com/68135957/221923082-c48ef4fd-a68a-46e4-a6b9-6d9859acd488.png)

Now that we have the root certificate are generated, we are ready to generate the client/device certificates.

### 3. Create Client (IoT Device) Certificate
Client certificates are device-specific. A client certificate is tied to a device name, which should match the logical device name configured on the Azure Portal. We will generate a certificate for a device named *myiotdevice1*, which we are going to create later in Azure IoT Hub.
- **Create Client (Device) Certificates Directory Structure:** While you are at the *rootca* folder on the command prompt, issue following commands:
  ```bash
    cd..
    mkdir devices
    cd devices
    mkdir db
  ```
- **Generate client certificate for myiotdevice1:** 
- The following commands (within the *devices* folder) create a private key, a CSR, and finally the actual certificate in CRT format. Optionally, you might also want to generate a certificate in PFX format from the CRT format to use it on certain devices/desktop applications to simulate a physical IoT device.  When prompted for the Common Name/FQDN, enter *myiotdevice1*: 
  ```bash     
    openssl rand -hex 16 > db/serial
    openssl genpkey -out myiotdevice1.key -algorithm RSA -pkeyopt rsa_keygen_bits:2048 
    openssl req -new -key myiotdevice1.key -out myiotdevice1.csr
    openssl ca -config ..\rootca\rootca.conf -in myiotdevice1.csr -out myiotdevice1.crt -extensions client_ext
    openssl pkcs12 -export -in myiotdevice1.crt -inkey myiotdevice1.key -out myiotdevice1.pfx
  ```
  
  Note: If you want to generate more than one client certificates, for example for a device named *mydevice2*, repeat the above 5 commands with the correct device name.
  
  Now the CMD window screenshot looks somewhat like this:
  ![image](https://user-images.githubusercontent.com/68135957/222236802-db37afa2-b4ff-43bb-9686-df60f5114cc3.png)

### 3. Create Azure IoT Hub and Import Server Certificate 
In this section, we are going to log in to Azure Portal and create an IoT Hub and add 2 virtual devices (mydevice1, mydevice2).  We will set the authentication type as Root CA-signed certificate-based auth and import into Azure the root certificate we generated in the rootca folder.
 - **Create Azure IoT Hub:**
    - Go to portal.azure.com, login, search for IoT Hub in the search box, and click Create (+) icon, and create an IoT Hub resource.
   ![image](https://user-images.githubusercontent.com/68135957/221958580-346b8798-332a-4b2c-808a-09e665cd61a6.png)
    - Under Networking, grant Public Access:
    ![image](https://user-images.githubusercontent.com/68135957/221958811-94a5769b-a7c5-4fab-a9f6-0145abdbcd06.png)

    - Under Management, set permission leval as "Shared access policy + RBAC":
    ![image](https://user-images.githubusercontent.com/68135957/221959016-26de225c-d9d5-4812-acb0-b7408c12b165.png)
 
    - Review and create.  Once done, click "Go to resource" button to go to the resource Overview page.  At the time of writing this, the Overview page displays a message that the Baltimore CyberTrust certificate will expire in 2025.  Since ours is a new implementation, click "What do I need to do" link and click "Migrate to DigiCert Global G2" button and follow instructions to update the certificate.
    - On the resource Overview Page, make a note of the Host name.  We will need this info to connect our devices to the hub. 
 - **Import Root Certificate**
    - On the resource page, click "Certificates" menu on the left side blade, and click the "Add" button.
    - In the "Certificates" blade that appears, give a certficate name (eg. MyRealmHubRootCertificate).  Browse to the IotCerts/rootca folder and import the *rootca.pem* file we created above.  Check the "Set certificate status to verified on upload" check box.  
    - Click Save. The screenshot looks like:

      ![image](https://user-images.githubusercontent.com/68135957/221980845-347f1b52-331b-459c-8ee5-3bf668e1e0b8.png)

    
- **Create Virtual Devices**
    - On the resource page, click "Devices" menu on the left side blade, and click "Add Device" button.
    - Specify *mydevice1* as the Device Id.  This name must match the FQDN we used when generating the device certificate above.
    - Use Authentication type as *X.509 CA Signed*.  Keep "Connect this device to an IoT Hub" as *Enabled*.
    - Click Save. The screenshot looks like: 
      
      ![image](https://user-images.githubusercontent.com/68135957/221964335-3006e430-15d8-446b-a194-8fb479cef513.png)
    
    - Repeat the above steps and create one more device (Device Id: *mydevice2*).
    
- **Create Shared Access Policies**
    - On the resource page, click "Shared access policies" menu on the left side blade, add click "Add shared access policy"
    - In the "Add shared access policy" blade that appears, enter a custom policy name (eg. MyRealmHubSAP) and check all read, write, and connect options (for testing only).  We will need this policy name later when we create shared access signature in client devices.
    - Click Add. The screenshot looks like:

      ![image](https://user-images.githubusercontent.com/68135957/221981014-49641ae9-0211-4c86-a0b5-2e5added8256.png)


