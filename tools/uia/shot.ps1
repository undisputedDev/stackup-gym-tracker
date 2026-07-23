param([string]$OutFile = "shot.png")

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32P {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

$proc = Get-Process StackUp.App -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$rect = New-Object Win32P+RECT
[Win32P]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
$scale = 1.25 # display DPI scaling; PrintWindow renders scaled content
$w = [int](($rect.Right - $rect.Left) * $scale)
$h = [int](($rect.Bottom - $rect.Top) * $scale)

$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
# 0x2 = PW_RENDERFULLCONTENT (needed for WinUI3/composition surfaces)
[Win32P]::PrintWindow($proc.MainWindowHandle, $hdc, 0x2) | Out-Null
$g.ReleaseHdc($hdc)
$bmp.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output "saved $OutFile ($w x $h)"
