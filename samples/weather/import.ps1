# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Check if running on Linux
if ($IsLinux) {
    # Prompt for the file path containing the base64 encoded certificate
    $filePath = Read-Host "Please enter the file path for the base64 encoded certificate"

    # Read the base64 encoded certificate from the file
    $base64Cert = Get-Content -Path $filePath -Raw

    # Convert base64 string to byte array
    $certBytes = [Convert]::FromBase64String($base64Cert)

    # Save the certificate to a temporary file
    $tempCertPath = "/tmp/tempCert.pem"
    [System.IO.File]::WriteAllBytes($tempCertPath, $certBytes)

    # Extract the subject name from the certificate
    $subjectName = openssl x509 -in $tempCertPath -noout -subject | sed -n 's/^.*CN=//p'

    # Import the certificate into the personal keyring
    $importCommand = "certutil -d sql:$HOME/.pki/nssdb -A -t 'P,,' -n '$subjectName' -i $tempCertPath"
    Invoke-Expression $importCommand

    # Clean up temporary file
    Remove-Item $tempCertPath
} else {
    # Prompt for the file path containing the base64 encoded certificate
    $filePath = Read-Host "Please enter the file path for the base64 encoded certificate"

    # Read the base64 encoded certificate from the file
    $base64Cert = Get-Content -Path $filePath -Raw

    # Convert base64 string to byte array
    $certBytes = [Convert]::FromBase64String($base64Cert)

    # Create a new X509Certificate2 object
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
    $cert.Import($certBytes)

    # Open the LocalMachine\My store
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("My", "LocalMachine")
    $store.Open("ReadWrite")

    # Add the certificate to the store with the subject name as the certificate name
    $store.Add($cert)

    # Close the store
    $store.Close()
}