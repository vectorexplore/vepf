# VePF

> Build and run Windows Presentation Foundation (WPF) applications natively on Linux.

[![Platform](https://img.shields.io/badge/platform-Linux%20x64-blue.svg)](#supported-platforms)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](#prerequisites)
[![Website](https://img.shields.io/badge/website-vepf.net-green.svg)](https://www.vepf.net)

**VePF** brings the Windows Presentation Foundation (WPF) runtime and rendering pipeline to Linux, enabling developers to build, port, and run standard C# and XAML desktop applications on Linux without rewriting their UI layer.

---

## Supported Platforms

* **Operating System:** Linux (Ubuntu, Debian, Fedora, RHEL, Rocky Linux, openSUSE Leap, SLES, Arch Linux, and other modern distributions)
* **Architecture:** `x64` (`x86_64`)
* **Display Servers:** Wayland and X11 (with Vulkan hardware acceleration or Lavapipe software fallback)

---

## Prerequisites

VePF requires the **.NET 10 SDK** (or the **.NET 10 Runtime** for running pre-compiled applications). See Microsoft's official guide to [Install .NET on Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux) for distribution-specific instructions.

For example, on Ubuntu:

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

---

## Installation

### 1. Download and Extract VePF

Download the latest Linux `x64` release archive (`vepf-10.0.12-linux-x64.tar.gz`) and its `.sha256` checksum from the repository's **Releases** page (also linked at [www.vepf.net](https://www.vepf.net)), then verify and extract it:

```bash
sha256sum -c vepf-10.0.12-linux-x64.tar.gz.sha256
tar -xzf vepf-10.0.12-linux-x64.tar.gz
cd vepf-10.0.12-linux-x64
```

### 2. Install System Dependencies

Run the dependency installer to configure the required native libraries (`libvips` with ImageMagick support, Vulkan ICDs, Wayland/X11/XKB libraries, and GStreamer):

```bash
sudo bash install-deps.sh
```

### 3. Install the VePF Runtime

Install the VePF native runtime and WPF desktop assemblies into your .NET 10 installation (use `sudo` if .NET 10 is installed in a system-wide directory such as `/usr/share/dotnet`):

```bash
sudo bash install-runtime.sh
```

---

## License & Activation

Getting started with VePF is fast and free. Unactivated installations run in evaluation mode; apply a free or commercial license file (`vepf_license.ini`) from [www.vepf.net](https://www.vepf.net) for uninterrupted sessions:

* **Personal Use:** **100% free forever** for individual developers, students, researchers, and non-commercial projects.
* **Commercial Use (Early Adopter Offer):** **Free for early adopters** — any commercial license acquired before **March 1, 2027**, is completely free.

For full details, see the [End User License Agreement](./LICENSE.txt), [Terms of Use](./TERMS.md), and [Third-Party Notices](./THIRD-PARTY-NOTICES.txt) (also available at [www.vepf.net](https://www.vepf.net)).

### Quick Activation Steps

1. **Generate your machine fingerprint**  
   Run `VepfLicenseTool` to display your unique hardware fingerprint (the C# source `VepfLicenseTool.cs` is also included and can be executed directly with `dotnet VepfLicenseTool.cs`):

   ```bash
   ./VepfLicenseTool
   ```

2. **Download your license file**  
   Visit [www.vepf.net](https://www.vepf.net), submit your machine fingerprint, and download your `vepf_license.ini` file.

3. **Apply the license**  
   Install `vepf_license.ini` into all detected `Microsoft.WindowsDesktop.App` runtime directories (defaults to `./vepf_license.ini` if no path is specified; prepend `sudo` if .NET 10 is installed system-wide):

   ```bash
   sudo ./VepfLicenseTool apply [path/to/vepf_license.ini]
   ```

---

## Getting Started

### Try the Included Demo

The extracted release archive includes a sample WPF application in the `demo` directory. You can build and launch it immediately from the extracted folder to verify your setup:

```bash
cd demo
dotnet run
```

### Create or Run Your Own WPF Application

Once installed, you can create, build, and run WPF applications on Linux using standard `.NET` CLI workflows:

```bash
# Create a new WPF application
dotnet new wpf -n MyLinuxWpfApp
cd MyLinuxWpfApp

# Build and run directly on Linux
dotnet run
```

Existing `.NET 10` WPF applications compiled on Windows (`net10.0-windows`) can also be executed directly on Linux with `dotnet YourApp.dll`.

---

## Known Limitations

VePF is under active development. The following legacy Windows-specific features are not currently supported:

* **Windows Forms Interop:** Hosting WinForms controls (`WindowsFormsHost`) and `System.Windows.Forms` integration.
* **XPS Documents:** XML Paper Specification (`System.Windows.Xps`) document rendering and printing.

---

## Uninstallation

To remove VePF runtime files and assemblies from your .NET 10 installation (prepend `sudo` if .NET 10 is installed system-wide):

```bash
sudo bash uninstall-runtime.sh
```