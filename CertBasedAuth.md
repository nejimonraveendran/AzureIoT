# How to create server and client certificates for certificate-based device authentication in Azure IoT
This document details how to create certificates for certificate-based authentication for IoT devices.  We will be creating a root certificate, which we will upload into Azure portal. We will also be creating a client certificate, which is to be used on the IoT device for authentication. 

Important Note: The certificate generation method uses OpenSSL CA signing with our own certificate authority, which is recommended for testing purpose only.  For production, use a proper public certificate authority (eg. Entrust). This tutorial assumes that these operations are carried out on a Windows machine.

This document is based on the official Microsoft documentation, but uses a simplified approach involving fewer steps, since we are doing it for testing.  The link is here for reference: https://learn.microsoft.com/en-us/azure/iot-hub/tutorial-x509-openssl

### 1. Install OpenSSL
If OpenSSL is not already installed, get **OpenSSL for Windows** from [here](https://wiki.openssl.org/index.php/Binaries).  Add the OpenSSL bin path it to Windows PATH environment variable.

### 2. Create Server (IoT Hub) Certificate
- In C drive, create a folder called IoTCerts, open CMD as Adminstrator within this folder.
- **Create root CA directory structure:** Issue following commands to create 
  ```bash
    mkdir rootca
    cd rootca
    mkdir certs db private
    type nul > db/index
    openssl rand -hex 16 > db/serial
    echo 1001 > db/crlnumber
  ```
  The window looks like the following:

  ![image](https://user-images.githubusercontent.com/68135957/221914468-0e5f6c69-a932-4621-a211-582d4454455b.png)
- **Create root.ca config file:** Open the *rootca* folder in Windows Explorer and create a file with the name *rootca.conf*.  Open that file in Notepad and paste the following content:

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
- Inside the file, change the domain_suffix, commonName, etc., according to your liking. Here is a completed sample file.
- **Create a root CA**:  2 steps here (both within the *rootca* folder. When prompted for a passphrase, enter a custom string and note it down):
  - Create a private key: ``` openssl req -new -config rootca.conf -out rootca.csr -keyout private/rootca.key ```
  - Create a self-signed certificate: ``` openssl ca -selfsign -config rootca.conf -in rootca.csr -out rootca.crt -extensions ca_ext ```
  - Create a PEM certificate out of CRT certificate just created: ``` openssl x509 -in rootca.crt -out rootca.pem -outform PEM ``` 
    Output window looks like the following:
    ![image](https://user-images.githubusercontent.com/68135957/221921095-5314fb8f-01c6-4ca3-88e2-6e81efca9d56.png)
  
    Now you can see a *rootca.crt* created in the *rootca* folder:
    ![image](https://user-images.githubusercontent.com/68135957/221923082-c48ef4fd-a68a-46e4-a6b9-6d9859acd488.png)
  
### 3. Create Client (IoT Device) Certificates
We will generate 2 certificates in the IotCerts\devices folder, for devices named *mydevice1* and *mydevice2*, both signed using the root certificate generated above.  Step are:
- Create directory structure IotCerts\devices:
  
  ![image](https://user-images.githubusercontent.com/68135957/221940830-ea43287a-445c-44c9-bedd-bc8f7af956ef.png)
 
- Create a private keys and certificate signing requests (CSR) for our devices using the following commands (from within the devices folder).  When prompted for the Common Name/FQDN, enter *mydevice1* and *mydevice2* in the following commands.  These device names are important, because we will need to use those when we create virtual devices on Azure IoT Hub later: 
  ```bash     
    openssl rand -hex 16 > db/serial
    openssl genpkey -out mydevice1.key -algorithm RSA -pkeyopt rsa_keygen_bits:2048 
    openssl req -new -key mydevice1.key -out mydevice1.csr
    openssl ca -config ..\rootca\rootca.conf -in mydevice1.csr -out mydevice1.crt -extensions client_ext
    openssl pkcs12 -export -in mydevice1.crt -inkey mydevice1.key -out mydevice1.pfx
    
    openssl rand -hex 16 > db/serial
    openssl genpkey -out mydevice2.key -algorithm RSA -pkeyopt rsa_keygen_bits:2048 
    openssl req -new -key mydevice2.key -out mydevice2.csr
    openssl ca -config ..\rootca\rootca.conf -in mydevice2.csr -out mydevice2.crt -extensions client_ext
    openssl pkcs12 -export -in mydevice2.crt -inkey mydevice2.key -out mydevice2.pfx  
  ```
  
  Sample CMD window screenshot looks like this:
  ![image](https://user-images.githubusercontent.com/68135957/221955110-afcb30cc-7fb2-4cb8-8321-9f946662a41d.png)


 ### 3. Create Azure IoT Hub 
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


