function workshop {
    if (Get-Command "steamcmd" -ErrorAction SilentlyContinue) {
        Write-Host "steamcmd is installed."
    } else {
        Write-Host "steamcmd is not installed."
        return
    }

    $STEAM_USER = Read-Host "Please enter your Steam username"

    if (-not $STEAM_USER) {
        Write-Host "Steam username cannot be empty. Unable to download workshop items."
        return
    }

    $workshopItemsFile = "RequiredWorkshopItems.txt"
    $workshopItems = ""

    if (Test-Path $workshopItemsFile) {
        $workshopItems = Get-Content $workshopItemsFile | Where-Object { $_ -ne "" } | ForEach-Object { "+workshop_download_item 294100 $_" }
        Write-Host "Workshop items: '$workshopItems'"
    } else {
        Write-Host "RequiredWorkshopItems.txt file not found. Nothing to download."
        return
    }

    # Set the download location to Workshop
    $scriptDirectory = $PSScriptRoot
    $downloadPath = Join-Path $scriptDirectory "Workshop"
    if (-not (Test-Path $downloadPath)) {
        New-Item -ItemType Directory -Path $downloadPath | Out-Null
    }

    # Download workshop items for appid 294100
    &steamcmd +force_install_dir $downloadPath +login $STEAM_USER $workshopItems +quit

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to download workshop item ID: $itemId"
    }

    Write-Host "Workshop items downloaded successfully."

}

# Call the function
workshop
Write-Host "Setup complete. Solution should be buildable now."