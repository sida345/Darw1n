using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static bool success = true;

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        DrawAscii();

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("\nEnter device UDID: ");
        string udid = Console.ReadLine()!.Trim();

        Console.Write("Enter path to com.apple.mobilegestalt.plist: ");
        string plistPath = Console.ReadLine()!.Trim();

        if (!File.Exists(plistPath))
            LogError("plist file not found!");

        string iosVersion = DetectiOS(udid);

        if (iosVersion == null)
        {
            LogError("Unable to iOS version!");
        }
        else
        {
            LogInfo("iOS version:" + iosVersion);

            if (IsUnsupported(iosVersion))
            {
                LogError("This iOS version is NOT supported by Darw1n!");
                Console.WriteLine("\nPress ENTER to exit.");
                Console.ReadLine();
                return;
            }
        }

        RunPython(udid, plistPath);

        Console.ResetColor();
        Console.WriteLine("\nDarw1n finished. Press ENTER to exit.");
        Console.ReadLine();
    }
    static void DrawAscii()
    {
        Console.ForegroundColor = ConsoleColor.White;

        string ascii = @"

           .                                
        .  x  .                             
      .$x.$xx.+x$.    ..:.xxX.              
      .xx .x+..$x   .xxxxxxxx$xxxxx.        
      $$   X$  .$x   xxx&xxxxXxXxxxxx.      
     .x$   $x   .x$  .$$xxxx$$xxXxxxxx .    
      xx . $x.  $x:    .xxX. X$xxxxxxxxxx   
      .xxxxxxxxxx; Xxx$xx+   .x$xxx$xxxx    
         ..:x;   &X&&XXXx.  &xxxxxxxxx...   
           .x$.    XXXX$X$$xxxxxxxxxx$.     
            xX       $XXXXXX$X$xxxX:        
            xx       $++X;X$xxxxx           
           $xxx;     xXXXxx+  xxx.          
           .Xxxxx;xxx::::xxxx&$xxx.         
            ;x.xxxxx:::::xxxxxx..           
            .xxXxxx::::::xxXxxxx.           
             x$   +:::::::xxXxxx.           
             xx   X:::::::xxxx.             
            .xx   ::::::::+xxxx             
             xx .&xx:::::$xxxxx             
             $x  &xxxx::xxxxxx&xxxxxxxx     
             Xx$XX&xxxxXxxxxx&xxxxxxx.      
             .$.$..   .XXX$$X+              
                       .. XX                

";
        Console.WriteLine(ascii);
        Console.WriteLine("\nDarw1n v0.1 GitHub: sida345");
    }
    static string DetectiOS(string udid)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ideviceinfo",
                Arguments = $"-u {udid} -k ProductVersion",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var p = Process.Start(psi);
            string output = p!.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();

            return string.IsNullOrEmpty(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }
    
    static bool IsUnsupported(string version)
    {
        Version ver = ParseVersion(version);
        
        Version maxSupported = new Version(26, 2, 1);

        return ver >= maxSupported;
    }

    static Version ParseVersion(string ver)
    {
        ver = ver.ToLower();
        ver = Regex.Replace(ver, @"beta\s*\d+", "");
        ver = ver.Trim();

        string[] parts = ver.Split('.');

        int major = parts.Length > 0 ? int.Parse(parts[0]) : 0;
        int minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
        int patch = parts.Length > 2 ? int.Parse(parts[2]) : 0;

        return new Version(major, minor, patch);
    }
    static void RunPython(string udid, string plist)
    {
        Console.WriteLine("\nRunning Darw1n...\n");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"run.py {udid} \"{plist}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                LogInfo(e.Data);
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                LogError(e.Data);
                success = false;
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode == 0 && success)
            {
                LogInfo("Darw1n completed successfully.");
                RestartDevice(udid);
            }
            else
            {
                LogError("Darw1n finished with errors. Device will NOT be rebooted.");
            }
        }
        catch (Exception ex)
        {
            LogError("Python launch error: " + ex.Message);
        }
    }
    static void RestartDevice(string udid)
    {
        Console.WriteLine();
        LogInfo("Rebooting the device...");

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "idevicediagnostics",
                Arguments = $"restart -u {udid}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var p = Process.Start(psi);
            string outp = p!.StandardOutput.ReadToEnd();
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (!string.IsNullOrEmpty(outp))
                LogInfo(outp);

            if (!string.IsNullOrEmpty(err))
                LogError(err);

            LogInfo("Device reboot command sent.");
        }
        catch (Exception ex)
        {
            LogError("Failed to reboot device: " + ex.Message);
        }
    }
    
    static void LogInfo(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[OK] " + msg);
        Console.ForegroundColor = ConsoleColor.White;
    }

    static void LogError(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[ERR] " + msg);
        Console.ForegroundColor = ConsoleColor.White;
    }
}