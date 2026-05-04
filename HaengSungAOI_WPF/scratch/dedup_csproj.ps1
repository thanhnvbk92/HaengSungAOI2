$path = "f:\Dev\Projects\AI Project\HaengSungAOI2\HaengSungAOI_WPF\HaengSungAOI_WPF.csproj"
$xml = [xml](Get-Content $path)

# Dictionary to keep track of seen Includes
$seen = @{}

# Select all ItemGroup children (Compile, Page, Resource, etc.)
$nodesToRemove = @()

foreach ($itemGroup in $xml.Project.ItemGroup) {
    $children = $itemGroup.ChildNodes | Where-Object { $_.Attributes["Include"] -ne $null }
    foreach ($node in $children) {
        $include = $node.Attributes["Include"].Value
        $key = "$($node.Name):$include"
        if ($seen.ContainsKey($key)) {
            $nodesToRemove += $node
        } else {
            $seen[$key] = $true
        }
    }
}

foreach ($node in $nodesToRemove) {
    $node.ParentNode.RemoveChild($node)
}

$xml.Save($path)
Write-Host "Deduplicated $path"
