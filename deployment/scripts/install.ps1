# MeldenIT Agent Installation Script
# PowerShell script for silent installation

param(
    [string]$ApiUrl = "https://assit.meldencloud.com",
    [string]$SnipeItUrl = "https://assit.meldencloud.com", 
    [string]$SiteCode = "MLDHQ",
    [string]$MsiPath = "MeldenITAgent.msi",
    [switch]$Force,
    [switch]$Uninstall
)

# Check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Check .NET 8 Runtime
function Test-DotNet8 {
    try {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -match "^8\.") {
            return $true
        }
    }
    catch {
        return $false
    }
    return $false
}

# Install .NET 8 Runtime
function Install-DotNet8 {
    Write-Host "Installing .NET 8 Runtime..." -ForegroundColor Yellow
    
    $dotnetUrl = "https://download.microsoft.com/download/8/8/5/885e5b0b-0b3b-4b0a-8b0a-8b0a8b0a8b0a/dotnet-runtime-8.0.0-win-x64.exe"
    $installerPath = "$env:TEMP\dotnet-runtime-8.0.0-win-x64.exe"
    
    try {
        Invoke-WebRequest -Uri $dotnetUrl -OutFile $installerPath
        Start-Process -FilePath $installerPath -ArgumentList "/quiet" -Wait
        Remove-Item $installerPath -Force
        Write-Host ".NET 8 Runtime installed successfully" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install .NET 8 Runtime: $_"
        exit 1
    }
}

# Uninstall existing version
function Uninstall-Existing {
    Write-Host "Checking for existing installation..." -ForegroundColor Yellow
    
    $existingProduct = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "MeldenIT Agent" }
    
    if ($existingProduct) {
        Write-Host "Found existing installation. Uninstalling..." -ForegroundColor Yellow
        $existingProduct.Uninstall()
        Start-Sleep -Seconds 5
    }
}

# Install MSI
function Install-Msi {
    param(
        [string]$MsiPath,
        [string]$ApiUrl,
        [string]$SnipeItUrl,
        [string]$SiteCode
    )
    
    Write-Host "Installing MeldenIT Agent..." -ForegroundColor Yellow
    
    $arguments = @(
        "/i", $MsiPath,
        "/quiet",
        "/norestart",
        "API_URL=`"$ApiUrl`"",
        "SNIPEIT_URL=`"$SnipeItUrl`"",
        "SITE_CODE=`"$SiteCode`""
    )
    
    try {
        $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $arguments -Wait -PassThru
        
        if ($process.ExitCode -eq 0) {
            Write-Host "MeldenIT Agent installed successfully" -ForegroundColor Green
        }
        else {
            Write-Error "Installation failed with exit code: $($process.ExitCode)"
            exit 1
        }
    }
    catch {
        Write-Error "Failed to install MeldenIT Agent: $_"
        exit 1
    }
}

# Configure agent
function Set-AgentConfig {
    param(
        [string]$ApiUrl,
        [string]$SnipeItUrl,
        [string]$SiteCode
    )
    
    Write-Host "Configuring agent..." -ForegroundColor Yellow
    
    $configPath = "$env:ProgramData\MeldenIT\Agent\agent.json"
    
    $config = @{
        api_url = $ApiUrl
        snipeit_url = $SnipeItUrl
        site_code = $SiteCode
        heartbeat_interval = 15
        delta_sync_interval = 360
        full_sync_time = "03:00"
        proxy_enabled = $false
        log_level = "Information"
        max_retry_attempts = 3
        retry_delay_seconds = 30
    }
    
    try {
        $configJson = $config | ConvertTo-Json -Depth 10
        $configJson | Out-File -FilePath $configPath -Encoding UTF8 -Force
        Write-Host "Agent configuration saved" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to save agent configuration: $_"
        exit 1
    }
}

# Start services
function Start-Services {
    Write-Host "Starting services..." -ForegroundColor Yellow
    
    try {
        # Start the Windows Service
        Start-Service -Name "MeldenITAgentSvc" -ErrorAction SilentlyContinue
        
        # Start tray application for current user
        $trayPath = "$env:ProgramFiles\MeldenIT\Agent\MeldenIT.Agent.Tray.exe"
        if (Test-Path $trayPath) {
            Start-Process -FilePath $trayPath -WindowStyle Hidden
        }
        
        Write-Host "Services started successfully" -ForegroundColor Green
    }
    catch {
        Write-Warning "Some services may not have started: $_"
    }
}

# Main installation logic
function Install-MeldenITAgent {
    Write-Host "MeldenIT Agent Installation Script" -ForegroundColor Cyan
    Write-Host "====================================" -ForegroundColor Cyan
    
    # Check administrator privileges
    if (-not (Test-Administrator)) {
        Write-Error "This script must be run as Administrator"
        exit 1
    }
    
    # Check .NET 8 Runtime
    if (-not (Test-DotNet8)) {
        Write-Host ".NET 8 Runtime not found" -ForegroundColor Yellow
        Install-DotNet8
    }
    else {
        Write-Host ".NET 8 Runtime is already installed" -ForegroundColor Green
    }
    
    # Uninstall existing version if requested
    if ($Force) {
        Uninstall-Existing
    }
    
    # Install MSI
    if (Test-Path $MsiPath) {
        Install-Msi -MsiPath $MsiPath -ApiUrl $ApiUrl -SnipeItUrl $SnipeItUrl -SiteCode $SiteCode
    }
    else {
        Write-Error "MSI file not found: $MsiPath"
        exit 1
    }
    
    # Configure agent
    Set-AgentConfig -ApiUrl $ApiUrl -SnipeItUrl $SnipeItUrl -SiteCode $SiteCode
    
    # Start services
    Start-Services
    
    Write-Host "`nInstallation completed successfully!" -ForegroundColor Green
    Write-Host "MeldenIT Agent is now running in the system tray." -ForegroundColor Green
}

# Uninstall logic
function Uninstall-MeldenITAgent {
    Write-Host "Uninstalling MeldenIT Agent..." -ForegroundColor Yellow
    
    try {
        # Stop services
        Stop-Service -Name "MeldenITAgentSvc" -Force -ErrorAction SilentlyContinue
        
        # Uninstall MSI
        $product = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -eq "MeldenIT Agent" }
        if ($product) {
            $product.Uninstall()
            Write-Host "MeldenIT Agent uninstalled successfully" -ForegroundColor Green
        }
        else {
            Write-Host "MeldenIT Agent not found" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Error "Failed to uninstall MeldenIT Agent: $_"
        exit 1
    }
}

# Main execution
if ($Uninstall) {
    Uninstall-MeldenITAgent
}
else {
    Install-MeldenITAgent
}
