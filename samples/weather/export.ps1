# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if ($Linux) {
    $certName = "mycert.crt"
    if ($args.Lenbth -eq 0 ) {
        openssl req -x509 -nodes -days 180 -newkey rsa:2048 -keyout mycert.key -out mycert.crt -subj "/CN=localhost"
    } else {
         $certName = $args[0]
    }
    
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($args[0])

    # Export the certificate to a byte array
    $certBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)

    # Convert the byte array to a Base64 string
    $base64Cert = [Convert]::ToBase64String($certBytes)

    # Print the Base64 encoded certificate
    Write-Host $base64Cert
    return
}

$subjectName = "localhost"
if ($args.Lenbth -eq 0 ) {
    # Define the subject name to search for
    $subjectName = $args[0]
}

# Get the certificate from the Local Personal Certificate Store
$cert = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*CN=$subjectName*" }

if ($cert) {
    # Export the certificate to a byte array
    $certBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)

    # Convert the byte array to a Base64 string
    $base64Cert = [Convert]::ToBase64String($certBytes)

    # Print the Base64 encoded certificate
    Write-Host $base64Cert
} else {
    Write-Host "Certificate not found."
}