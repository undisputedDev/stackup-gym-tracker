Add-Type -AssemblyName System.Drawing

$base = "$env:LOCALAPPDATA\Temp\claude\c--work-Gameprojects-StackUp---Simple-Gym-Tracker\cfa4ad31-119f-4b39-b011-4296391ba0b7\scratchpad"
$shots = "$base\shots"
$outDir = "$base\store_screenshots"
New-Item -ItemType Directory -Force -Path "$outDir\en" | Out-Null
New-Item -ItemType Directory -Force -Path "$outDir\de" | Out-Null

$teal = [System.Drawing.ColorTranslator]::FromHtml("#2E6E62")
$white = [System.Drawing.Color]::White
$bandH = 380

# Special chars built from codepoints to keep this source file pure ASCII
$dash = [char]0x2013       # en dash
$aLow = [char]0x00E4       # a-umlaut
$uLow = [char]0x00FC       # u-umlaut (lower)
$uUp  = [char]0x00DC       # U-umlaut (upper)
$szlig = [char]0x00DF      # sharp s

$caps = @{}
$caps["en_1_home"]    = "Pick a workout and start"
$caps["en_2_session"] = "Your next weight, suggested automatically"
$caps["en_3_stats"]   = "Watch every lift trend upward"
$caps["en_4_splits"]  = "Ready-made splits, fully customizable"
$caps["en_5_finish"]  = "Finish $dash next session plan included"
$caps["de_1_home"]    = "Training w${aLow}hlen und loslegen"
$caps["de_2_session"] = "Dein n${aLow}chstes Gewicht $dash automatisch vorgeschlagen"
$caps["de_3_stats"]   = "Dein Fortschritt, ${uUp}bung f${uLow}r ${uUp}bung"
$caps["de_4_splits"]  = "Fertige Splits, voll anpassbar"
$caps["de_5_finish"]  = "Abschlie${szlig}en $dash Plan f${uLow}rs n${aLow}chste Mal"

foreach ($key in $caps.Keys) {
  $src = "$shots\$key.png"
  if (-not (Test-Path $src)) { Write-Host "MISSING $src"; continue }
  $img = [System.Drawing.Image]::FromFile($src)
  $w = $img.Width; $h = $img.Height
  $canvas = New-Object System.Drawing.Bitmap ($w, ($h + $bandH))
  $g = [System.Drawing.Graphics]::FromImage($canvas)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
  $brushTeal = New-Object System.Drawing.SolidBrush $teal
  $g.FillRectangle($brushTeal, 0, 0, $w, $bandH)
  $g.DrawImage($img, 0, $bandH, $w, $h)
  $font = New-Object System.Drawing.Font("Segoe UI Semibold", 46, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
  $sf = New-Object System.Drawing.StringFormat
  $sf.Alignment = [System.Drawing.StringAlignment]::Center
  $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
  $brushWhite = New-Object System.Drawing.SolidBrush $white
  $pad = 70
  $rect = New-Object System.Drawing.RectangleF ($pad, 0, ($w - 2*$pad), $bandH)
  $g.DrawString($caps[$key], $font, $brushWhite, $rect, $sf)
  $g.Dispose()
  $lang = $key.Substring(0,2)
  $outName = $key.Substring(3)
  $outPath = "$outDir\$lang\$outName.png"
  $canvas.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
  $canvas.Dispose(); $img.Dispose()
  Write-Host "wrote $lang/$outName.png"
}
Write-Host "DONE"
