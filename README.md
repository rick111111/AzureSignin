# AzureSignin

1. Install Nuget packages Microsoft.Azure.Services.AppAuthentication (from https://securitytools.pkgs.visualstudio.com/_packaging/ASAL/nuget/v3/index.json) and Microsoft.Azure.KeyVault
    - [x] There is a public preview version of Microsoft.Azure.Services.AppAuthentication which is much newer, we need to update the code to work against that version
2. Run powershell script to create a service principal with certificate
    ```powershell
    $cert = New-SelfSignedCertificate -CertStoreLocation "cert:\LocalMachine\My" -Subject "CN=exampleappScriptCert" -KeySpec KeyExchange
    $keyValue = [System.Convert]::ToBase64String($cert.GetRawCertData())

    $sp = New-AzureRMADServicePrincipal -DisplayName faxue-exampleapp2 -CertValue $keyValue -EndDate $cert.NotAfter -StartDate $cert.NotBefore
    Sleep 10
    New-AzureRmRoleAssignment -RoleDefinitionName Contributor -ServicePrincipalName $sp.ApplicationId
    ```
3. Mark the application id of the new created service principal, and use it later as the ClientId
4. Note
    - After certificate is uploaded as .pfx file to Key Vault, I can't find a way to retrive the certificate back with private key. The CertificateBundle we get back will only contain public key which is not enough to get access token from Azure.
    - The only way that works here is to export the certificate as PFX content, save the bytearray as base64 format string, and upload to Key Vault as a secret string
        ```powershell
        $byteCert = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx)
        $strCert = [System.Convert]::ToBase64String($byteCert)
        $strCert > 'cert5.txt'
        [io.file]::WriteAllBytes('cert5.pfx', $byteCert)
        ```
5. Trouble Shooting
    - When failed to load binaries, check the fusionlog in exception
    - On CryptographicException of message "Keyset does not exist", try run the app as admin, it might because non-admin app not able to load private key from local machine