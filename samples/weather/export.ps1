# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Check if running on Linux
if ($IsLinux) {
    # Prompt for the certificate name
    $certName = Read-Host "Please enter the certificate name (default: localhost)"
    if ([string]::IsNullOrWhiteSpace($certName)) {
        $certName = "localhost"
    }

    # Check if the certificate exists using certutil
    $certExists = certutil -d sql:$HOME/.pki/nssdb -L | Select-String -Pattern $certName

    if ($certExists) {
        # Export the certificate to a temporary file
        $tempCertPath = "/tmp/tempCert.pem"
        $exportCommand = "certutil -d sql:$HOME/.pki/nssdb -L -n '$certName' -a > $tempCertPath"
        Invoke-Expression $exportCommand

        # Read the certificate from the temporary file
        $certBytes = [System.IO.File]::ReadAllBytes($tempCertPath)

        # Convert the certificate to a base64 string
        $base64Cert = [Convert]::ToBase64String($certBytes)

        # Write the base64 encoded certificate to a file named [subjectname].key
        $keyFilePath = "$certName.key"
        Set-Content -Path $keyFilePath -Value $base64Cert

        # Clean up temporary file
        Remove-Item $tempCertPath
    } else {
        Write-Host "Certificate not found."
    }
} else {
    # Open the Personal store
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("My", "CurrentUser")
    $store.Open("ReadOnly")

    # Prompt for the certificate name
    $certName = Read-Host "Please enter the certificate name (default: localhost)"
    if ([string]::IsNullOrWhiteSpace($certName)) {
        $certName = "localhost"
    }

    # Check if the certificate exists in the Personal store
    $cert = $store.Certificates | Where-Object { $_.Subject -like "*CN=$certName*" }

    if ($cert) {
        # Export the certificate to a byte array
        $certBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)

        # Convert the certificate to a base64 string
        $base64Cert = [Convert]::ToBase64String($certBytes)

        # Write the base64 encoded certificate to a file named [subjectname].key
        $keyFilePath = "$certName.key"
        Set-Content -Path $keyFilePath -Value $base64Cert
    } else {
        Write-Host "Certificate not found."
    }

    # Close the store
    $store.Close()
}