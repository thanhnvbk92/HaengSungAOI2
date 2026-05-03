$csprojPath = "f:\Dev\Projects\AI Project\HaengSungAOI2\HaengSungAOI_WPF\HaengSungAOI_WPF.csproj"
$xml = [xml](Get-Content $csprojPath)
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("ns", "http://schemas.microsoft.com/developer/msbuild/2003")

$vmAssemblyPath = "C:\Program Files\VisionMaster4.2.0\Development\V4.x\ComControls\Assembly"

$standardRefs = @("System", "System.Data", "System.Xml", "Microsoft.CSharp", "System.Core", "System.Xml.Linq", 
                 "System.Data.DataSetExtensions", "System.Net.Http", "System.Xaml", "WindowsBase", 
                 "PresentationCore", "PresentationFramework", "System.Drawing", "System.Windows.Forms", 
                 "System.Transactions", "System.Web.Extensions", "System.ComponentModel.DataAnnotations", 
                 "System.Numerics", "mscorlib")

# Update existing HintPaths and add missing ones for VisionMaster and other libs
$references = $xml.SelectNodes("//ns:Reference", $ns)

foreach ($ref in $references) {
    $include = $ref.Include
    $name = $include.Split(',')[0].Trim()
    
    # Skip standard references and those that already have a HintPath (except if it's the bad one)
    $hintPath = $ref.SelectSingleNode("ns:HintPath", $ns)
    
    if ($standardRefs -contains $name) {
        continue
    }

    if ($null -ne $hintPath) {
        # Fix the bad relative path if it's there
        if ($hintPath.InnerText -match "Program Files") {
            $hintPath.InnerText = Join-Path $vmAssemblyPath ($name + ".dll")
        }
        continue
    }

    # If it's a VM module or other potentially missing lib, add HintPath
    if ($name -match "ModuCs$|ModuleCs$|^VM|^IMVS|^Apps\.|^FrontendUI|^Image|^String|^Translation|^Graphics|^Coordinate|^Read|^Data|^Save|^Rotate|^If|^Point|^Trigger|^Shell|^Time|^Comm|^And|^Calculator") {
        $dllName = "$name.dll"
        $fullPath = Join-Path $vmAssemblyPath $dllName
        
        # Check if the DLL actually exists in the VM assembly folder before adding HintPath
        if (Test-Path $fullPath) {
            $hintPath = $xml.CreateElement("HintPath", "http://schemas.microsoft.com/developer/msbuild/2003")
            $ref.AppendChild($hintPath)
            $hintPath.InnerText = $fullPath
        }
    }
}

# Remove the AssemblySearchPaths I added earlier to avoid confusion
$searchPathNode = $xml.SelectSingleNode("//ns:AssemblySearchPaths", $ns)
if ($null -ne $searchPathNode) {
    $searchPathNode.ParentNode.RemoveChild($searchPathNode)
}

# Save the updated csproj
$xml.Save($csprojPath)
Write-Host "CSPROJ updated with absolute HintPaths for all missing non-standard references."
