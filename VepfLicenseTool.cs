// VePF License & Activation Utility
// Run directly via `dotnet VepfLicenseTool.cs` or use the pre-compiled `./VepfLicenseTool` binary.
//
// For transparency, `./VepfLicenseTool` is compiled directly from this file using .NET 10+:
// dotnet publish VepfLicenseTool.cs -c Release -r linux-x64 --no-self-contained -p:PublishAot=false -p:PublishTrimmed=false -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o .

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public class VepfLicenseTool
{
    private const string ScriptName = "./VepfLicenseTool";
    private const string DefaultLicenseFileName = "vepf_license.ini";
    private const string LicensePortalUrl = "https://www.vepf.net";

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowFingerprintAndHelp();
            return;
        }

        string command = args[0].Trim().ToLowerInvariant();

        switch (command)
        {
            case "fingerprint":
            case "--fingerprint":
            case "-f":
                ShowFingerprintAndHelp();
                break;

            case "apply":
            case "--apply":
            case "-a":
                string licensePath = args.Length >= 2 ? args[1] : DefaultLicenseFileName;
                ApplyLicense(licensePath);
                break;

            case "help":
            case "--help":
            case "-h":
                PrintUsage();
                break;

            default:
                Console.Error.WriteLine($"Unknown option: {args[0]}");
                PrintUsage();
                Environment.Exit(1);
                break;
        }
    }

    private static void ShowFingerprintAndHelp()
    {
        string fingerprint = ComputeFingerprint();

        Console.WriteLine($"VePF Fingerprint: {fingerprint}");
        Console.WriteLine();
        Console.WriteLine("Privacy Notice:");
        Console.WriteLine("  No sensitive or personally identifiable machine information is collected or transmitted.");
        Console.WriteLine("  Each hardware attribute is salted with the \"VePF\" prefix prior to one-way SHA-256 hashing");
        Console.WriteLine("  and truncated to 32 bits, preventing reverse lookup or reconstruction of your hardware IDs.");
        Console.WriteLine();
        Console.WriteLine("How to activate your VePF license:");
        Console.WriteLine($"  1. Visit {LicensePortalUrl} and obtain a free permanent personal license or commercial license using the fingerprint above.");
        Console.WriteLine($"  2. Save the downloaded license file as '{DefaultLicenseFileName}'.");
        Console.WriteLine($"  3. Apply it to all installed Microsoft.WindowsDesktop.App runtime directories:");
        Console.WriteLine($"       {ScriptName} apply [path/to/{DefaultLicenseFileName}]");
    }

    private static void ApplyLicense(string sourceLicensePath)
    {
        string fullSourcePath = Path.GetFullPath(sourceLicensePath);
        if (!File.Exists(fullSourcePath))
        {
            Fail($"License file not found: {fullSourcePath}");
        }

        if (new FileInfo(fullSourcePath).Length == 0)
        {
            Fail($"License file is empty: {fullSourcePath}");
        }

        List<string> targetDirs = FindWindowsDesktopAppVersionDirs();
        if (targetDirs.Count == 0)
        {
            Fail("No Microsoft.WindowsDesktop.App runtime directories were found.");
        }

        int copiedCount = 0;

        foreach (string versionDir in targetDirs)
        {
            string destPath = Path.Combine(versionDir, DefaultLicenseFileName);
            try
            {
                File.Copy(fullSourcePath, destPath, overwrite: true);

                // Ensure non-root users can read the license even when installed via sudo with a strict umask
                if (OperatingSystem.IsLinux())
                {
                    File.SetUnixFileMode(
                        destPath,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite |
                        UnixFileMode.GroupRead | UnixFileMode.OtherRead);
                }

                Console.WriteLine($"[OK] Installed license to: {destPath}");
                copiedCount++;
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"[Permission Denied] Could not write to: {destPath} (try running with sudo)");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Error] Failed to copy to {destPath}: {ex.Message}");
            }
        }

        if (copiedCount == 0)
        {
            Fail("Failed to apply license to any Microsoft.WindowsDesktop.App directory.");
        }

        Console.WriteLine($"Successfully applied {DefaultLicenseFileName} to {copiedCount} runtime version(s).");
    }

    private static List<string> FindWindowsDesktopAppVersionDirs()
    {
        var dirs = new HashSet<string>(StringComparer.Ordinal);

        // Resolve the active `dotnet` host executable if invoked via `dotnet`
        string dotnetExe = "dotnet";
        if (!string.IsNullOrEmpty(Environment.ProcessPath))
        {
            string resolved = File.ResolveLinkTarget(Environment.ProcessPath, returnFinalTarget: true)?.FullName
                              ?? Environment.ProcessPath;
            if (Path.GetFileNameWithoutExtension(resolved).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                dotnetExe = resolved;
            }
        }

        // 1. Primary: Query `dotnet --list-runtimes`
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = dotnetExe,
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);

                var regex = new Regex(@"^Microsoft\.WindowsDesktop\.App\s+(\S+)\s+\[(.+)\]$", RegexOptions.Multiline);
                foreach (Match match in regex.Matches(output))
                {
                    string version = match.Groups[1].Value.Trim();
                    string basePath = match.Groups[2].Value.Trim();
                    string fullVersionDir = Path.Combine(basePath, version);

                    if (Directory.Exists(fullVersionDir))
                    {
                        dirs.Add(fullVersionDir);
                    }
                }
            }
        }
        catch
        {
            // Fallback below if `dotnet --list-runtimes` fails
        }

        // 2. Fallback: Check active dotnet host root, DOTNET_ROOT, and standard install locations
        if (dirs.Count == 0)
        {
            var candidateRoots = new List<string?>
            {
                dotnetExe != "dotnet" ? Path.GetDirectoryName(dotnetExe) : null,
                Environment.GetEnvironmentVariable("DOTNET_ROOT"),
                Environment.GetEnvironmentVariable("DOTNET_ROOT_X64"),
                "/usr/share/dotnet"
            };

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                candidateRoots.Add(Path.Combine(home, ".dotnet"));
            }

            foreach (string? root in candidateRoots.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                string winDesktopRoot = Path.Combine(root!, "shared", "Microsoft.WindowsDesktop.App");
                if (Directory.Exists(winDesktopRoot))
                {
                    foreach (string verDir in Directory.GetDirectories(winDesktopRoot))
                    {
                        dirs.Add(verDir);
                    }
                }
            }
        }

        return dirs.OrderBy(d => d, StringComparer.Ordinal).ToList();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine($"  {ScriptName} [fingerprint | -f]        Display machine fingerprint and online license instructions (default)");
        Console.WriteLine($"  {ScriptName} apply [license_file]      Install license file (default: {DefaultLicenseFileName}) to all Microsoft.WindowsDesktop.App versions");
        Console.WriteLine($"  {ScriptName} help                      Show this help message");
    }

    // Must remain identical to the fingerprint verification algorithm in libmilcorez.so
    private static string ComputeFingerprint()
    {
        string? mobo = GetMachineId();
        if (mobo is null or "") Fail("Machine ID not found.");

        string? mac = GetPrimaryMacAddress();
        if (mac is null or "") Fail("MAC Address not found.");

        string? cpu = GetCpuModel();
        if (cpu is null or "") Fail("CPU Model not found.");

        string hMobo = HashComponent(mobo);
        string hMac = HashComponent(mac);
        string hCpu = HashComponent(cpu);

        return $"{hMobo}-{hMac}-{hCpu}";
    }

    [DoesNotReturn]
    private static void Fail(string reason)
    {
        Console.Error.WriteLine($"Error: {reason}");
        Environment.Exit(1);
    }

    // Prefixes ("salts") each value with "VePF" before SHA-256 hashing and truncates to 32 bits
    // so raw hardware values cannot be reversed via precomputed hash/rainbow tables.
    private static string HashComponent(string input)
    {
        string inputH = "VePF" + input;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(inputH));
        return Convert.ToHexString(hash, 0, 4);
    }

    private static string? GetMachineId()
    {
        if (File.Exists("/etc/machine-id"))
        {
            try { return File.ReadAllText("/etc/machine-id").Trim(); } catch { }
        }
        if (File.Exists("/var/lib/dbus/machine-id"))
        {
            try { return File.ReadAllText("/var/lib/dbus/machine-id").Trim(); } catch { }
        }
        return null;
    }

    private static string? GetPrimaryMacAddress()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                              nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .Where(mac => !string.IsNullOrEmpty(mac))
                .OrderBy(mac => mac)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static string? GetCpuModel()
    {
        if (File.Exists("/proc/cpuinfo"))
        {
            try
            {
                return File.ReadAllLines("/proc/cpuinfo")
                    .FirstOrDefault(line => line.StartsWith("model name") || line.StartsWith("Hardware"))
                    ?.Split(':')
                    .LastOrDefault()
                    ?.Trim();
            }
            catch { }
        }
        return null;
    }
}