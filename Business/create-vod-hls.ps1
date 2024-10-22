$START_TIME = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "-----START GENERATING HLS STREAM-----"

# Usage check
if (-not $args[0]) {
    Write-Host "Usage: create-vod-hls.ps1 SOURCE_FILE [OUTPUT_NAME]"
    exit 1
}

# Renditions array (resolution, bitrate, audio-rate)
$renditions = @(
    "426x240 400k 128k",
    "640x360 800k 128k",
    "842x480 1400k 192k",
    "1280x720 2800k 192k",
    "1920x1080 5000k 256k",
    "2560x1440 12000k 384k",
    "3840x2160 35000k 384k"
)

$segment_target_duration = 10
$max_bitrate_ratio = 1.07
$rate_monitor_buffer_ratio = 1.5

$source = $args[0]
$target = $args[1]
if (-not $target) {
    $target = [System.IO.Path]::GetFileNameWithoutExtension($source)
}

Write-Host "Source: $source"
Write-Host "Target: $target"

# Create target directory
New-Item -ItemType Directory -Force -Path $target | Out-Null

# Get source resolution using FFprobe
$sourceResolution = & ffprobe -v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 $source
$sourceDimensions = $sourceResolution -split "x"
$sourceWidth = [int]$sourceDimensions[0]
$sourceHeight = [int]$sourceDimensions[1]

Write-Host "Source Width: $sourceWidth"
Write-Host "Source Height: $sourceHeight"

# Get source audio bitrate
$sourceAudioBitRate = (& ffprobe -v error -select_streams a:0 -show_entries stream=bit_rate -of csv=s=x:p=0 $source).Trim()
$sourceAudioBitRateFormatted = [math]::Floor([int]$sourceAudioBitRate / 1000)

# Calculate keyframe interval
$key_frames_interval = [math]::Round(((& ffprobe $source 2>&1 | Select-String -Pattern '\d+(.\d+)? fps' -AllMatches).Matches[0].Value -replace ' fps', '') * 2)
$key_frames_interval = [math]::Round($key_frames_interval / 10) * 10

# Static parameters for FFmpeg
$static_params = "-c:a aac -ac 2 -ar 48000 -c:s webvtt -c:v h264_amf -pix_fmt yuv420p -profile:v main -crf 19 -sc_threshold 0"
$static_params += " -g $key_frames_interval -keyint_min $key_frames_interval -hls_time $segment_target_duration -hls_playlist_type vod"

# Miscellaneous parameters
$misc_params = "-hide_banner -y"

# Master playlist header
$master_playlist = @"
#EXTM3U
#EXT-X-VERSION:3
"@

$cmd = ""
$resolutionValid = $false
$prevHeight = 0

# Process each rendition
foreach ($rendition in $renditions) {
    $rendition = $rendition -replace '\s+', ' '  # Normalize spaces
    $fields = $rendition -split ' '
    $resolution = $fields[0]
    $bitrate = $fields[1]
    $audiorate = $fields[2]

    $audioBitRateFormatted = [int]($audiorate -replace 'k$', '')

    if ($audioBitRateFormatted -gt $sourceAudioBitRateFormatted) {
        $audiorate = "$sourceAudioBitRateFormatted`k"
    }

    $width = [int]($resolution -split 'x')[0]
    $height = [int]($resolution -split 'x')[1]

    if ($sourceHeight -le $prevHeight) {
        Write-Host "Video source has height smaller than output height ($height)"
        continue
    }

    $bitrateValue = [int]($bitrate -replace 'k$', '')  # Remove the 'k' and convert to integer

    $maxrate = [math]::Round($bitrateValue * $max_bitrate_ratio)
    $bufsize = [math]::Round($bitrateValue * $rate_monitor_buffer_ratio)
    $bandwidth = $bitrateValue * 1000
    $name = "${height}p"

    if ($width / $sourceWidth * $sourceHeight -gt $height) {
        $widthParam = -2
        $heightParam = $height
    } else {
        $widthParam = $width
        $heightParam = -2
    }
    Write-Host "WidthParam: $widthParam"
    Write-Host "HeightParam: $heightParam"

    $cmd += " $static_params -vf scale=w=" + $widthParam + ":h=" + $heightParam

    # $cmd += " $static_params -vf scale=w=$widthParam:h=$heightParam"
    $cmd += " -b:v $bitrate -maxrate ${maxrate}k -bufsize ${bufsize}k -b:a $audiorate"
    $cmd += " -hls_segment_filename `"$target/$name`_%03d.ts`" `"$target/$name.m3u8`""

    # Add rendition to master playlist
    $master_playlist += "#EXT-X-STREAM-INF:BANDWIDTH=$bandwidth,RESOLUTION=$resolution`n$name.m3u8`n"

    $resolutionValid = $true
    $prevHeight = $height
}

if ($resolutionValid) {
    # Start the FFmpeg command
    Write-Host "Executing command:"
    Write-Host "ffmpeg -hwaccel auto $misc_params -i `$source` $cmd"
    $full_cmd = "ffmpeg -hwaccel auto $misc_params -i `"$source`" $cmd"
    Invoke-Expression $full_cmd

    # Create master playlist file
    Set-Content -Path "$target/playlist.m3u8" -Value $master_playlist
    Write-Host "Done - encoded HLS is at $target/"
} else {
    Write-Host "Video source is too small"
    exit 1
}

$elapsed_time = $START_TIME.Elapsed.TotalSeconds
Write-Host "Elapsed time: $elapsed_time seconds"
Write-Host "-----FINISH GENERATING HLS STREAM-----"
