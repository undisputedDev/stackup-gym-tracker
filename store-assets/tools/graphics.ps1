Add-Type -AssemblyName System.Drawing

$outDir = "$env:LOCALAPPDATA\Temp\claude\c--work-Gameprojects-StackUp---Simple-Gym-Tracker\cfa4ad31-119f-4b39-b011-4296391ba0b7\scratchpad\graphics"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$teal   = [System.Drawing.ColorTranslator]::FromHtml("#2E6E62")
$tealLt = [System.Drawing.ColorTranslator]::FromHtml("#3C8577")
$tealDk = [System.Drawing.ColorTranslator]::FromHtml("#255A50")
$white  = [System.Drawing.Color]::White
$markUp = [System.Drawing.ColorTranslator]::FromHtml("#5FC98A")  # lightened up-green for contrast on teal
$markKp = [System.Drawing.ColorTranslator]::FromHtml("#C9CDD3")
$markDn = [System.Drawing.ColorTranslator]::FromHtml("#E58078")

# --- Draw the white dumbbell + up-arrow foreground, scaled from the 456-unit SVG ---
# $s = scale factor from SVG units to target; $ox/$oy = offset of the 456 box within target
function Draw-Mark($g, $s, $ox, $oy, $brush) {
  # up arrow: polygon points 228,100 274,152 248,152 248,186 208,186 208,152 182,152
  $pts = @(
    (New-Object System.Drawing.PointF (($ox+228*$s),($oy+100*$s))),
    (New-Object System.Drawing.PointF (($ox+274*$s),($oy+152*$s))),
    (New-Object System.Drawing.PointF (($ox+248*$s),($oy+152*$s))),
    (New-Object System.Drawing.PointF (($ox+248*$s),($oy+186*$s))),
    (New-Object System.Drawing.PointF (($ox+208*$s),($oy+186*$s))),
    (New-Object System.Drawing.PointF (($ox+208*$s),($oy+152*$s))),
    (New-Object System.Drawing.PointF (($ox+182*$s),($oy+152*$s)))
  )
  $g.FillPolygon($brush, $pts)
  # helper for rounded rects
  function RR($x,$y,$w,$h,$r) {
    $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2*$r
    $gp.AddArc($x, $y, $d, $d, 180, 90)
    $gp.AddArc($x+$w-$d, $y, $d, $d, 270, 90)
    $gp.AddArc($x+$w-$d, $y+$h-$d, $d, $d, 0, 90)
    $gp.AddArc($x, $y+$h-$d, $d, $d, 90, 90)
    $gp.CloseFigure()
    return $gp
  }
  # bar 150,238 156x22 r8 ; plates 116,200 32x98 r11 & 308,200 ; caps 92,221 20x56 r8 & 344,221
  $rects = @(
    @(150,238,156,22,8), @(116,200,32,98,11), @(308,200,32,98,11), @(92,221,20,56,8), @(344,221,20,56,8)
  )
  foreach ($r in $rects) {
    $gp = RR ($ox+$r[0]*$s) ($oy+$r[1]*$s) ($r[2]*$s) ($r[3]*$s) ($r[4]*$s)
    $g.FillPath($brush, $gp)
    $gp.Dispose()
  }
}

# ============ 1. APP ICON 512x512 ============
$icon = New-Object System.Drawing.Bitmap (512, 512)
$g = [System.Drawing.Graphics]::FromImage($icon)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$brushTeal = New-Object System.Drawing.SolidBrush $teal
$g.FillRectangle($brushTeal, 0, 0, 512, 512)
$brushWhite = New-Object System.Drawing.SolidBrush $white
$sc = 512.0 / 456.0
Draw-Mark $g $sc 0 0 $brushWhite
$g.Dispose()
$icon.Save("$outDir\icon_512.png", [System.Drawing.Imaging.ImageFormat]::Png)
$icon.Dispose()
Write-Host "wrote icon_512.png"

# ============ 2. FEATURE GRAPHIC 1024x500 ============
$fw = 1024; $fh = 500
$fg = New-Object System.Drawing.Bitmap ($fw, $fh)
$g = [System.Drawing.Graphics]::FromImage($fg)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
# diagonal teal gradient background
$rectF = New-Object System.Drawing.Rectangle (0,0,$fw,$fh)
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rectF, $tealDk, $tealLt, 25.0)
$g.FillRectangle($grad, $rectF)

# Right side: icon glyph in a soft rounded tile
$tileSize = 300
$tileX = 690; $tileY = ($fh - $tileSize)/2
$tileBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(38, 255, 255, 255))
$gpTile = New-Object System.Drawing.Drawing2D.GraphicsPath
$rr = 46; $dd = 2*$rr
$gpTile.AddArc($tileX, $tileY, $dd, $dd, 180, 90)
$gpTile.AddArc($tileX+$tileSize-$dd, $tileY, $dd, $dd, 270, 90)
$gpTile.AddArc($tileX+$tileSize-$dd, $tileY+$tileSize-$dd, $dd, $dd, 0, 90)
$gpTile.AddArc($tileX, $tileY+$tileSize-$dd, $dd, $dd, 90, 90)
$gpTile.CloseFigure()
$g.FillPath($tileBrush, $gpTile)
# dumbbell inside the tile (scale 456 -> tileSize*0.8, centered)
$glyphScale = ($tileSize*0.82) / 456.0
$glyphOx = $tileX + ($tileSize - 456*$glyphScale)/2
$glyphOy = $tileY + ($tileSize - 456*$glyphScale)/2
Draw-Mark $g $glyphScale $glyphOx $glyphOy $brushWhite

# Left side: wordmark + tagline + marks row
$fontTitle = New-Object System.Drawing.Font("Segoe UI", 92, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawString("StackUp", $fontTitle, $brushWhite, 70, 150)
$fontTag = New-Object System.Drawing.Font("Segoe UI Semilight", 38, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$tagBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
$g.DrawString("Simple gym tracker with auto-progression", $fontTag, $tagBrush, 74, 268)

# marks row: filled triangles up/keep/down
$fontMark = New-Object System.Drawing.Font("Segoe UI", 52, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$my = 340
$bUp = New-Object System.Drawing.SolidBrush $markUp
$bKp = New-Object System.Drawing.SolidBrush $markKp
$bDn = New-Object System.Drawing.SolidBrush $markDn
# up triangle
$g.FillPolygon($bUp, @(
  (New-Object System.Drawing.PointF (100,($my+50))),
  (New-Object System.Drawing.PointF (140,($my+50))),
  (New-Object System.Drawing.PointF (120,($my+8)))))
# keep bar
$g.FillRectangle($bKp, 175, ($my+26), 44, 14)
# down triangle
$g.FillPolygon($bDn, @(
  (New-Object System.Drawing.PointF (255,($my+8))),
  (New-Object System.Drawing.PointF (295,($my+8))),
  (New-Object System.Drawing.PointF (275,($my+50)))))
$g.DrawString("up  -  keep  -  down", $fontTag, $tagBrush, 320, ($my+2))

$g.Dispose()
$fg.Save("$outDir\feature_1024x500.png", [System.Drawing.Imaging.ImageFormat]::Png)
$fg.Dispose()
Write-Host "wrote feature_1024x500.png"
Write-Host "DONE"
