param(
    [Parameter(Mandatory)][int]$EditIndex,
    [Parameter(Mandatory)][string]$Value
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process StackUp.App -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)

$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Edit)
$edits = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
Write-Output "found $($edits.Count) edit controls"
if ($EditIndex -ge $edits.Count) { Write-Output "index out of range"; exit 1 }

$el = $edits[$EditIndex]
$vp = $null
if ($el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
    $vp.SetValue($Value)
    Write-Output "set edit[$EditIndex] = '$Value'"
} else {
    Write-Output "no ValuePattern on edit[$EditIndex]"
    exit 1
}
