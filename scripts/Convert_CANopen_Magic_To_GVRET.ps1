class Converter {
    static [string]$regexHeader = '^"([^"]+)"(?:,"([^"]+)")*$'

    [string] $srcFilename
    [string] $tgtFilename
    Converter([string]$filename) {
        $this.srcFilename = $filename

        $this.tgtFilename = (Join-Path (Join-Path . 'GVRET') (Split-Path -Leaf $this.srcFilename))
    }

    hidden [string] ConvertHeader([string]$line) {
        #Write-Host -ForegroundColor DarkBlue "[$line]"

        $matchInfo = ($line | Select-String -Pattern ([Converter]::regexHeader))
        if (-not $matchInfo) {
            pause
        }

        $this.columns = @(
            $matchInfo.Matches.Groups[1].Captures.Value
            )
        $matchInfo.Matches.Groups[2].Captures | ForEach-Object {
            $this.columns += $_.Value
        }
        #$this.columns | Format-List | Out-String | Write-Host -ForegroundColor DarkCyan

        for ($i = 0; $i -lt $this.columns.Length; $i++)
        {
            $this.columnsMap.Add($this.columns[$i], $i)
        }
        #$this.columnsMap | Format-Table | Out-String | Write-Host -ForegroundColor DarkCyan

        return 'Time Stamp,ID,Extended,Bus,LEN,D1,D2,D3,D4,D5,D6,D7,D8'
    }

    hidden [string] ConvertData([string]$line) {
        #Write-Host -ForegroundColor DarkBlue "$line"

        [string[]]$fields = ($line -split ',')

        $rawTimestamp = $fields[$this.columnsMap['Time']]
        $rawId = $fields[$this.columnsMap['ID']]
        $rawLen = $fields[$this.columnsMap['Length']]
        $rawData = $fields[$this.columnsMap['Raw Message']]
        #Write-Host -ForegroundColor DarkCyan $rawTimestamp
        #Write-Host -ForegroundColor DarkCyan $rawId
        #Write-Host -ForegroundColor DarkCyan $rawLen
        #Write-Host -ForegroundColor DarkCyan $rawData

        $rawTimestamp -match '^"(\d+):(\d+):(\d+):(\d+\.\d+)''"$' | Out-Null
        [uint32]$days = $Matches[1]
        #Write-Host -ForegroundColor DarkCyan $days
        [uint32]$hours = $Matches[2]
        #Write-Host -ForegroundColor DarkCyan $hours
        [uint32]$minutes = $Matches[3]
        #Write-Host -ForegroundColor DarkCyan $minutes
        [double]$seconds = $Matches[4]
        #Write-Host -ForegroundColor DarkCyan $seconds

        [uint64]$timestamp = $days
        $timestamp *= 24
        $timestamp += $hours
        $timestamp *= 60
        $timestamp += $minutes
        $timestamp *= 60
        $timestamp *= 1000000
        $timestamp += $seconds * 1000000
        #Write-Host -ForegroundColor DarkCyan $timestamp

        $id = $rawId.Trim('"').Replace("0x","").PadLeft(8,'0')
        $extended = 'false'
        $bus = 0
        $len = $rawLen.Trim('"')
        $data = (($rawData.Trim('"') -split ' ') -join ',')

        return "$timestamp,$id,$extended,$bus,$len,$data"
    }

    hidden [string] ConvertLine([string]$line) {
        if ((-not $this.columns) -and $line.StartsWith('"Message Number"')) {
            return $this.ConvertHeader($line)
        } else {
            return $this.ConvertData($line)
        }
    }

    [string[]]$columns = $null
    $columnsMap = @{}
    [void] Convert() {
        if (-not (Test-Path $this.tgtFilename)) {
            Write-Host -ForegroundColor DarkGray "$($this.srcFilename) -> $($this.tgtFilename)"

            [string]$tgtDirectory = (Split-Path -Parent $this.tgtFilename)
            if (-not (Test-Path -PathType Container $tgtDirectory)) {
                New-Item -ItemType Directory $tgtDirectory | Out-Null
            }

            Remove-Item -ErrorAction SilentlyContinue $this.tgtFilename

            [string[]]$content = (Get-Content $this.srcFilename)

            $content = $content[4..($content.Length-1)]

            Write-Progress -Activity Convert -Status $this.srcFilename
            for ($i = 0; $i -lt $content.Count; $i++) {
                Write-Progress -Activity Convert -Status $this.srcFilename -PercentComplete (100 * $i / $content.Count)
                $line = $content[$i]
                $this.ConvertLine($line) | Out-File -Encoding ascii -Append -FilePath $this.tgtFilename
            }
            Write-Progress -Activity Convert -Status $this.srcFilename -Completed
        }
    }
}

$originalFiles = @( (Get-ChildItem -Filter *.csv).FullName )
#$originalFiles = @( 'C:\Model3Can\first3.csv' )

$originalFiles | ForEach-Object {
    $converter = New-Object Converter($_)
    $converter.Convert()
}
