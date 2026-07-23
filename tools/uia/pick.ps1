param([Parameter(Mandatory)][string]$ItemName)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process StackUp.App -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)

$comboCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ComboBox)
$combo = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $comboCond)
if ($null -eq $combo) { Write-Output "no combobox"; exit 1 }

$exp = $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
$exp.Expand()
Start-Sleep -Milliseconds 800

$itemCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, $ItemName)
$item = $combo.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $itemCond)
if ($null -eq $item) {
    # popup can be hosted outside the combo; search from the root
    $item = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $itemCond)
}
if ($null -eq $item) { Write-Output "item '$ItemName' not found"; $exp.Collapse(); exit 1 }

$sel = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
$sel.Select()
Write-Output "picked '$ItemName'"
