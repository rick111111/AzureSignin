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
3. Register an application in AAD and get the ID