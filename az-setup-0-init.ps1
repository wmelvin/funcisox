# ----------------------------------------------------------------------
#  az-setup-0-init.ps1
#
#  Read settings from a file outside the project tree (may include
#  secrets). Initialize variables used to configure Azure resources in
#  other scripts.
# ----------------------------------------------------------------------

# Source function definitions.
. ./az-funcs.ps1


# - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
# Set parameters:

# -- Set $USE_APP_SERVICE_PLAN = $false to use a Consumption Plan.
#
$USE_APP_SERVICE_PLAN = $true
# $USE_APP_SERVICE_PLAN = $false


# -- Get key variables from file in local encrypted folder.

$profilePath = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
$keysFile = [System.IO.Path]::Combine($profilePath, "KeepLocal", "funcisox-settings.ps1")

# -- Source the file to set the vars.
. $keysFile

if (0 -eq $SGKey.Length) {
    Write-Host "Failed to get SGKey from '$keysFile'."
    Exit 1
}

if (0 -eq $EmailRecipientAddress.Length) {
  Write-Host "Failed to get EmailRecipientAddress from '$keysFile'."
  Exit 1
}

if (0 -eq $EmailSenderAddress.Length) {
  Write-Host "Failed to get EmailSenderAddress from '$keysFile'."
  Exit 1
}


# -- Assign vars for script.
$baseName = "func01"
$rgName = "${baseName}-rg"
$location = "eastus"
$funcAppName = "funcisox"
$storageAcctName = "${baseName}storage"
$appInsightsName = "${baseName}insights"

$funcPlanName = "${baseName}-plan"
$planSku = "EP1"

#  Assign variables for use by the file upload webapp. Funcisox uses a Windows
#  plan, so a separate Linux plan is created for the file upload app.
$appServiceName = "${baseName}linuxplan"  
$webAppName = "${baseName}webapp"


#$preserveTempFiles = "True"
$preserveTempFiles = ""

Say "INFO:            rgName    = '$rgName'"
Say "INFO:          location    = '$location'"
Say "INFO: USE_APP_SERVICE_PLAN = $USE_APP_SERVICE_PLAN"
Say "INFO:       funcAppName    = '$funcAppName '"
Say "INFO:   storageAcctName    = '$storageAcctName'"
Say "INFO:   appInsightsName    = '$appInsightsName'"
Say "INFO:      funcPlanName    = '$funcPlanName'"
Say "INFO:           planSku    = '$planSku'"
Say "INFO: preserveTempFiles    = '$preserveTempFiles'"
Say "INFO:    appServiceName    = '$appServiceName'"
Say "INFO:        webAppName    = '$webAppName'"
