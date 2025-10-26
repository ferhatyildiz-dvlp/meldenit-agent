# MeldenIT Agent Build Script
# PowerShell script for building the agent and creating MSI

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\dist",
    [switch]$Clean,
    [switch]$Publish,
    [switch]$CreateMsi
)

# Build configuration
$SolutionPath = ".\agent\MeldenIT.Agent.sln"
$MsiOutputPath = ".\deployment\wix\MeldenITAgent.msi"

function Write-Header {
    param([string]$Message)
    Write-Host "`n$Message" -ForegroundColor Cyan
    Write-Host ("=" * $Message.Length) -ForegroundColor Cyan
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    
    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Host ".NET SDK Version: $dotnetVersion" -ForegroundColor Green
    }
    catch {
        Write-Error ".NET SDK not found. Please install .NET 8 SDK."
        exit 1
    }
    
    # Check WiX Toolset (if creating MSI)
    if ($CreateMsi) {
        try {
            $wixVersion = & "candle.exe" -? 2>&1 | Select-String "Windows Installer XML"
            if ($wixVersion) {
                Write-Host "WiX Toolset found" -ForegroundColor Green
            }
            else {
                Write-Warning "WiX Toolset not found. MSI creation will be skipped."
                $CreateMsi = $false
            }
        }
        catch {
            Write-Warning "WiX Toolset not found. MSI creation will be skipped."
            $CreateMsi = $false
        }
    }
}

function Invoke-Clean {
    Write-Header "Cleaning Solution"
    
    if (Test-Path $OutputPath) {
        Remove-Item $OutputPath -Recurse -Force
        Write-Host "Output directory cleaned" -ForegroundColor Green
    }
    
    # Clean solution
    dotnet clean $SolutionPath --configuration $Configuration
    Write-Host "Solution cleaned" -ForegroundColor Green
}

function Invoke-Build {
    Write-Header "Building Solution"
    
    # Restore packages
    Write-Host "Restoring packages..." -ForegroundColor Yellow
    dotnet restore $SolutionPath
    
    # Build solution
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build $SolutionPath --configuration $Configuration --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed"
        exit 1
    }
    
    Write-Host "Build completed successfully" -ForegroundColor Green
}

function Invoke-Test {
    Write-Header "Running Tests"
    
    $testProject = ".\agent\MeldenIT.Agent.Tests\MeldenIT.Agent.Tests.csproj"
    
    if (Test-Path $testProject) {
        Write-Host "Running tests..." -ForegroundColor Yellow
        dotnet test $testProject --configuration $Configuration --no-build --verbosity normal
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Some tests failed"
        }
        else {
            Write-Host "All tests passed" -ForegroundColor Green
        }
    }
    else {
        Write-Host "No test project found" -ForegroundColor Yellow
    }
}

function Invoke-Publish {
    Write-Header "Publishing Applications"
    
    # Create output directory
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force
    }
    
    # Publish Service
    Write-Host "Publishing Service..." -ForegroundColor Yellow
    $serviceProject = ".\agent\MeldenIT.Agent.Service\MeldenIT.Agent.Service.csproj"
    $serviceOutput = "$OutputPath\Service"
    
    dotnet publish $serviceProject `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained true `
        --output $serviceOutput `
        --no-build
    
    # Publish Tray Application
    Write-Host "Publishing Tray Application..." -ForegroundColor Yellow
    $trayProject = ".\agent\MeldenIT.Agent.Tray\MeldenIT.Agent.Tray.csproj"
    $trayOutput = "$OutputPath\Tray"
    
    dotnet publish $trayProject `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained true `
        --output $trayOutput `
        --no-build
    
    # Publish Main Application
    Write-Host "Publishing Main Application..." -ForegroundColor Yellow
    $mainProject = ".\agent\MeldenIT.Agent\MeldenIT.Agent.csproj"
    $mainOutput = "$OutputPath\Main"
    
    dotnet publish $mainProject `
        --configuration $Configuration `
        --runtime win-x64 `
        --self-contained true `
        --output $mainOutput `
        --no-build
    
    Write-Host "Publishing completed successfully" -ForegroundColor Green
}

function New-Msi {
    Write-Header "Creating MSI Package"
    
    $wixPath = ".\deployment\wix"
    $wxsFile = "$wixPath\MeldenITAgent.wxs"
    
    if (-not (Test-Path $wxsFile)) {
        Write-Error "WiX source file not found: $wxsFile"
        return
    }
    
    # Compile WiX source
    Write-Host "Compiling WiX source..." -ForegroundColor Yellow
    & "candle.exe" -out "$wixPath\MeldenITAgent.wixobj" $wxsFile
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "WiX compilation failed"
        return
    }
    
    # Link WiX object
    Write-Host "Linking WiX object..." -ForegroundColor Yellow
    & "light.exe" -out $MsiOutputPath "$wixPath\MeldenITAgent.wixobj"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "WiX linking failed"
        return
    }
    
    if (Test-Path $MsiOutputPath) {
        Write-Host "MSI created successfully: $MsiOutputPath" -ForegroundColor Green
    }
    else {
        Write-Error "MSI creation failed"
    }
}

function Copy-ConfigFiles {
    Write-Header "Copying Configuration Files"
    
    $configSource = ".\deployment\config"
    $configDest = "$OutputPath\config"
    
    if (Test-Path $configSource) {
        if (-not (Test-Path $configDest)) {
            New-Item -ItemType Directory -Path $configDest -Force
        }
        
        Copy-Item "$configSource\*" $configDest -Recurse -Force
        Write-Host "Configuration files copied" -ForegroundColor Green
    }
    else {
        Write-Host "No configuration files found" -ForegroundColor Yellow
    }
}

function New-Archive {
    Write-Header "Creating Archive"
    
    $archivePath = "$OutputPath\MeldenITAgent.zip"
    
    if (Test-Path $archivePath) {
        Remove-Item $archivePath -Force
    }
    
    Compress-Archive -Path "$OutputPath\*" -DestinationPath $archivePath -Force
    Write-Host "Archive created: $archivePath" -ForegroundColor Green
}

# Main execution
Write-Host "MeldenIT Agent Build Script" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

# Check prerequisites
Test-Prerequisites

# Clean if requested
if ($Clean) {
    Invoke-Clean
}

# Build solution
Invoke-Build

# Run tests
Invoke-Test

# Publish if requested
if ($Publish) {
    Invoke-Publish
    Copy-ConfigFiles
    New-Archive
}

# Create MSI if requested
if ($CreateMsi) {
    New-Msi
}

Write-Host "`nBuild process completed!" -ForegroundColor Green
