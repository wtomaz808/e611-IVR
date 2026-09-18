param(
    [string]$Source = (Join-Path $PSScriptRoot '..\docs\azure-services-mcp-integration.svg')
)

$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path $Source).Path
$sourceDirectory = Split-Path $sourcePath
$drawioPath = [System.IO.Path]::ChangeExtension($sourcePath, '.drawio')
$visioSvgPath = Join-Path $sourceDirectory 'azure-services-mcp-integration-visio.svg'

$readerSettings = [System.Xml.XmlReaderSettings]::new()
$readerSettings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
$readerSettings.XmlResolver = $null
$reader = [System.Xml.XmlReader]::Create($sourcePath, $readerSettings)
$svg = [System.Xml.XmlDocument]::new()
$svg.PreserveWhitespace = $true
try {
    $svg.Load($reader)
}
finally {
    $reader.Dispose()
}

function Escape-XmlAttribute([string]$Value) {
    return [System.Security.SecurityElement]::Escape($Value)
}

function Get-Number([System.Xml.XmlElement]$Element, [string]$Name) {
    $value = $Element.GetAttribute($Name)
    if ([string]::IsNullOrWhiteSpace($value)) { return 0 }
    return [double]::Parse($value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Format-Number([double]$Value) {
    return $Value.ToString('0.###', [System.Globalization.CultureInfo]::InvariantCulture)
}

function Get-RectStyle([string]$ClassName) {
    $styles = @{
        bg    = 'fillColor=#f7f9fc;strokeColor=none;'
        scope = 'fillColor=#ffffff;strokeColor=#94a3b8;strokeWidth=1.5;rounded=1;'
        az    = 'fillColor=#ffffff;strokeColor=#0078d4;strokeWidth=2;rounded=1;'
        tm    = 'fillColor=#faf8ff;strokeColor=#6264a7;strokeWidth=2;rounded=1;'
        c     = 'fillColor=#ffffff;strokeColor=#cbd5e1;strokeWidth=1.3;rounded=1;'
        p     = 'fillColor=#fffaf0;strokeColor=#d97706;strokeWidth=1.6;rounded=1;dashed=1;dashPattern=7 5;'
        head  = 'fillColor=#e8f3fb;strokeColor=none;'
        alt   = 'fillColor=#f8fafc;strokeColor=none;'
    }
    $style = $styles[$ClassName]
    if (-not $style) { $style = $styles.c }
    return "shape=rectangle;whiteSpace=wrap;html=1;$style"
}

function Get-TextStyle([string]$ClassName) {
    $definitions = @{
        t  = @{ Size = 27; Bold = 1; Align = 'left'; Color = '#0f172a' }
        s  = @{ Size = 14; Bold = 0; Align = 'left'; Color = '#475569' }
        h  = @{ Size = 15; Bold = 1; Align = 'left'; Color = '#1e293b' }
        n  = @{ Size = 13; Bold = 1; Align = 'center'; Color = '#0f172a' }
        d  = @{ Size = 11; Bold = 0; Align = 'center'; Color = '#475569' }
        st = @{ Size = 10; Bold = 1; Align = 'center'; Color = '#16794b' }
        l  = @{ Size = 10; Bold = 0; Align = 'center'; Color = '#334155' }
        th = @{ Size = 11; Bold = 1; Align = 'left'; Color = '#0f172a' }
        td = @{ Size = 10.5; Bold = 0; Align = 'left'; Color = '#334155' }
    }
    $definition = $definitions[$ClassName]
    if (-not $definition) { $definition = $definitions.d }
    $fontStyle = if ($definition.Bold) { 1 } else { 0 }
    return @{
        Size = [double]$definition.Size
        Align = $definition.Align
        Style = "text;html=1;strokeColor=none;fillColor=none;whiteSpace=wrap;overflow=visible;align=$($definition.Align);verticalAlign=middle;fontFamily=Segoe UI;fontSize=$($definition.Size);fontStyle=$fontStyle;fontColor=$($definition.Color);spacing=0;"
    }
}

function Get-PathPoints([string]$Data) {
    $tokens = [regex]::Matches($Data, '[A-Za-z]|-?\d+(?:\.\d+)?') | ForEach-Object Value
    $points = [System.Collections.Generic.List[object]]::new()
    $x = 0.0
    $y = 0.0
    $index = 0
    while ($index -lt $tokens.Count) {
        $command = $tokens[$index++]
        switch ($command.ToUpperInvariant()) {
            'M' {
                $x = [double]::Parse($tokens[$index++], [System.Globalization.CultureInfo]::InvariantCulture)
                $y = [double]::Parse($tokens[$index++], [System.Globalization.CultureInfo]::InvariantCulture)
                $points.Add([pscustomobject]@{ X = $x; Y = $y })
            }
            'L' {
                $x = [double]::Parse($tokens[$index++], [System.Globalization.CultureInfo]::InvariantCulture)
                $y = [double]::Parse($tokens[$index++], [System.Globalization.CultureInfo]::InvariantCulture)
                $points.Add([pscustomobject]@{ X = $x; Y = $y })
            }
            'H' {
                $x = [double]::Parse($tokens[$index++], [System.Globalization.CultureInfo]::InvariantCulture)
                $points.Add([pscustomobject]@{ X = $x; Y = $y })
            }
            'V' {
                $y = [double]::Parse($tokens[$index++], [System.Globalization.CultureInfo]::InvariantCulture)
                $points.Add([pscustomobject]@{ X = $x; Y = $y })
            }
        }
    }
    return $points
}

$cells = [System.Text.StringBuilder]::new()
[void]$cells.AppendLine('<mxCell id="0"/>')
[void]$cells.AppendLine('<mxCell id="1" parent="0"/>')
$cellId = 2
$nodes = $svg.SelectNodes('//*[not(ancestor::*[local-name()="defs"]) and (local-name()="rect" or local-name()="text" or local-name()="image" or local-name()="path")]')

foreach ($node in $nodes) {
    $element = [System.Xml.XmlElement]$node
    $localName = $element.LocalName
    $classList = $element.GetAttribute('class')
    $className = ($classList -split '\s+')[0]
    if ($localName -eq 'rect') {
        $x = Get-Number $element 'x'
        $y = Get-Number $element 'y'
        $width = Get-Number $element 'width'
        $height = Get-Number $element 'height'
        $style = Get-RectStyle $className
        [void]$cells.AppendLine("<mxCell id=`"$cellId`" value=`"`" style=`"$style`" vertex=`"1`" parent=`"1`"><mxGeometry x=`"$(Format-Number $x)`" y=`"$(Format-Number $y)`" width=`"$(Format-Number $width)`" height=`"$(Format-Number $height)`" as=`"geometry`"/></mxCell>")
    }
    elseif ($localName -eq 'text') {
        $definition = Get-TextStyle $className
        $x = Get-Number $element 'x'
        $baseline = Get-Number $element 'y'
        $height = [math]::Max(18, $definition.Size * 1.55)
        $width = if ($definition.Align -eq 'center') { 360 } elseif ($className -eq 't') { 1200 } else { 900 }
        if ($definition.Align -eq 'center') { $x -= $width / 2 }
        $y = $baseline - ($height * 0.78)
        $value = Escape-XmlAttribute $element.InnerText
        $style = $definition.Style
        if ($className -eq 'st' -and $classList.Contains('plan')) {
            $style = $style.Replace('#16794b', '#b45309')
        }
        [void]$cells.AppendLine("<mxCell id=`"$cellId`" value=`"$value`" style=`"$style`" vertex=`"1`" parent=`"1`"><mxGeometry x=`"$(Format-Number $x)`" y=`"$(Format-Number $y)`" width=`"$(Format-Number $width)`" height=`"$(Format-Number $height)`" as=`"geometry`"/></mxCell>")
    }
    elseif ($localName -eq 'image') {
        $href = $element.GetAttribute('href')
        $imagePath = Join-Path $sourceDirectory $href
        $base64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($imagePath))
        $dataUri = "data:image/svg+xml;base64,$base64"
        $x = Get-Number $element 'x'
        $y = Get-Number $element 'y'
        $width = Get-Number $element 'width'
        $height = Get-Number $element 'height'
        $style = Escape-XmlAttribute "shape=image;imageAspect=0;aspect=fixed;image=$dataUri;"
        [void]$cells.AppendLine("<mxCell id=`"$cellId`" value=`"`" style=`"$style`" vertex=`"1`" parent=`"1`"><mxGeometry x=`"$(Format-Number $x)`" y=`"$(Format-Number $y)`" width=`"$(Format-Number $width)`" height=`"$(Format-Number $height)`" as=`"geometry`"/></mxCell>")
    }
    elseif ($localName -eq 'path') {
        $points = @(Get-PathPoints $element.GetAttribute('d'))
        if ($points.Count -lt 2) { continue }
        $strokeColor = if ($className -eq 'pr') { '#0078d4' } elseif ($className -eq 'ds') { '#64748b' } else { '#334155' }
        $strokeWidth = if ($className -eq 'pr') { 2.4 } elseif ($className -eq 'ds') { 1.4 } else { 1.5 }
        $dashed = if ($className -eq 'ds') { 'dashed=1;dashPattern=6 5;' } else { '' }
        $style = "edgeStyle=none;rounded=0;orthogonalLoop=0;jettySize=auto;html=1;endArrow=block;endFill=1;strokeColor=$strokeColor;strokeWidth=$strokeWidth;$dashed"
        $sourcePoint = $points[0]
        $targetPoint = $points[-1]
        [void]$cells.AppendLine("<mxCell id=`"$cellId`" value=`"`" style=`"$style`" edge=`"1`" parent=`"1`"><mxGeometry relative=`"1`" as=`"geometry`"><mxPoint x=`"$(Format-Number $sourcePoint.X)`" y=`"$(Format-Number $sourcePoint.Y)`" as=`"sourcePoint`"/>")
        if ($points.Count -gt 2) {
            [void]$cells.AppendLine('<Array as="points">')
            foreach ($point in $points[1..($points.Count - 2)]) {
                [void]$cells.AppendLine("<mxPoint x=`"$(Format-Number $point.X)`" y=`"$(Format-Number $point.Y)`"/>")
            }
            [void]$cells.AppendLine('</Array>')
        }
        [void]$cells.AppendLine("<mxPoint x=`"$(Format-Number $targetPoint.X)`" y=`"$(Format-Number $targetPoint.Y)`" as=`"targetPoint`"/></mxGeometry></mxCell>")
    }
    $cellId++
}

$drawio = @"
<?xml version="1.0" encoding="UTF-8"?>
<mxfile host="app.diagrams.net" agent="Architecture converter" version="24.7.17">
  <diagram id="azure-mcp-integration" name="Architecture">
    <mxGraphModel dx="1800" dy="1200" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="1800" pageHeight="1200" math="0" shadow="0">
      <root>
$($cells.ToString())      </root>
    </mxGraphModel>
  </diagram>
</mxfile>
"@
[System.IO.File]::WriteAllText($drawioPath, $drawio, [System.Text.UTF8Encoding]::new($false))

$visioSvg = $svg.CloneNode($true)
foreach ($image in $visioSvg.SelectNodes('//*[local-name()="image"]')) {
    $hrefAttribute = @($image.Attributes | Where-Object LocalName -eq 'href')[0]
    $imagePath = Join-Path $sourceDirectory $hrefAttribute.Value
    $dataUri = 'data:image/svg+xml;base64,' + [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($imagePath))
    foreach ($attribute in @($image.Attributes | Where-Object LocalName -eq 'href')) {
        $attribute.Value = $dataUri
    }
}
$writerSettings = [System.Xml.XmlWriterSettings]::new()
$writerSettings.Encoding = [System.Text.UTF8Encoding]::new($false)
$writerSettings.Indent = $true
$writerSettings.OmitXmlDeclaration = $false
$writer = [System.Xml.XmlWriter]::Create($visioSvgPath, $writerSettings)
try {
    $visioSvg.Save($writer)
}
finally {
    $writer.Dispose()
}

Write-Output "Created $drawioPath"
Write-Output "Created $visioSvgPath"