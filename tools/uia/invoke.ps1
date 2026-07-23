param(
    [Parameter(Mandatory)][string]$Name,
    [string]$ControlType = "Button"
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process StackUp.App -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)

$typeMap = @{
    "Button" = [System.Windows.Automation.ControlType]::Button
    "TabItem" = [System.Windows.Automation.ControlType]::TabItem
    "Text" = [System.Windows.Automation.ControlType]::Text
}

$cond = New-Object System.Windows.Automation.AndCondition(
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name)),
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $typeMap[$ControlType]))
)
$el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
if ($null -eq $el) { Write-Output "NOT FOUND: $ControlType '$Name'"; exit 1 }

$invoke = $null
if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invoke)) {
    $invoke.Invoke()
    Write-Output "invoked $ControlType '$Name'"
} else {
    $select = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$select)) {
        $select.Select()
        Write-Output "selected $ControlType '$Name'"
    } else {
        Write-Output "no invoke/select pattern on '$Name'"
        exit 1
    }
}
