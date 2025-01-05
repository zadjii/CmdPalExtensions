$gitRoot = git rev-parse --show-toplevel
$DestinationFolder = Join-Path $gitRoot "x64\tmp"

$params = @{
    Endpoint = "https://eus.codesigning.azure.net/"
    CodeSigningAccountName = "zadjii-signing-cmdpalext"
    CertificateProfileName = "zadjii-cmdpalext-cert-profile"
    FilesFolder = $DestinationFolder
    FilesFolderFilter = "msix"
    FileDigest = "SHA256"
    TimestampRfc3161 = "http://timestamp.acs.microsoft.com"
    TimestampDigest = "SHA256"

    ExcludeEnvironmentCredential = $true
    ExcludeManagedIdentityCredential = $true 
    ExcludeSharedTokenCacheCredential = $true 
    ExcludeVisualStudioCredential = $true 
    ExcludeVisualStudioCodeCredential = $true 
    ExcludeAzureCliCredential = $false 
    ExcludeAzurePowershellCredential = $true 
    ExcludeInteractiveBrowserCredential = $true
}

Invoke-TrustedSigning @params