# ======================================================================
#  az-setup-4-fileup-deploy.ps1
# ----------------------------------------------------------------------

# -- Source the initialization script.
. ./az-setup-0-init.ps1

# -- Deploy the webapp ZIP file.
#    https://learn.microsoft.com/en-us/cli/azure/webapp?view=azure-cli-latest#az-webapp-deploy

if ($fileupDeployZipFile) {
    if ( [IO.File]::Exists($fileupDeployZipFile) ) {
        az webapp deploy --name $webAppName -g $rgName --src-path $fileupDeployZipFile
    }
    else {
        Yell "File not found: '$fileupDeployZipFile'"
    }
}
else {
    Yell "Failed to get 'fileupDeployZipFile' setting from '$keysFile'."
}


# ======================================================================
