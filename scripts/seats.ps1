# Development only. Opens one Chrome window for every seat at a table, so a
# game of three can be played, and watched, on one screen.
#
#   powershell -File scripts/seats.ps1 3
#
# Each window is a browser of its own. A profile directory of its own means a
# cookie jar of its own, and the cookie is the only thing the server knows a
# player by - so the windows cannot tell on each other, and the server cannot
# tell they share a machine. Sign each one in as a different account: a player
# holds at most one seat, so the same account twice gets a seat and a refusal.
#
# The profiles outlive the run and live outside the repository, under
# LOCALAPPDATA. A window opened again tomorrow is still signed in as whoever
# signed in to it today.
#
# `--app` is what allows a window this narrow at all. A window with tabs has a
# minimum width of five hundred pixels built into Chrome, because the tab strip
# and the address bar have to fit; a phone is three hundred and sixty. Dropping
# both drops the floor with them. It also means no address bar, which is why
# signing in through Google does not work here - use an email and a password.

param(
    [int] $Count = 2,
    [string] $Url = 'https://localhost:57541',
    [int] $Width = 360,
    [int] $Height = 760
)

$ErrorActionPreference = 'Stop'

if ($Count -lt 1) {
    throw "A table needs a seat; $Count were asked for."
}

# Where the installers put it: machine-wide first, then the per-user install
# that someone without administrator rights ends up with.
$Places = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
    "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
)

$chrome = $Places | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($null -eq $chrome) {
    throw "Chrome is in none of the usual places: $($Places -join ', ')"
}

$seats = Join-Path $env:LOCALAPPDATA 'rummy51\seats'

# Side by side while they fit, and then a new row begun far enough down that
# the title bar of every window under it is still there to be taken hold of.
$PerRow = 4
$Gap = 8
$Drop = 36

for ($seat = 1; $seat -le $Count; $seat++) {
    $place = $seat - 1
    $x = ($place % $PerRow) * ($Width + $Gap)
    $y = [math]::Floor($place / $PerRow) * $Drop

    $directory = Join-Path $seats "seat-$seat"

    # Only the directory is quoted, and it is the only one of these that can
    # hold a space: a user's name reaches it through LOCALAPPDATA.
    $arguments = @(
        "--app=$Url"
        "--user-data-dir=`"$directory`""
        "--window-size=$Width,$Height"
        "--window-position=$x,$y"
        # A profile created a moment ago asks to be made the default browser
        # and offers to import from another one. Neither is a question about
        # this game, and both are in the way of the window behind them.
        '--no-first-run'
        '--no-default-browser-check'
    )

    Start-Process -FilePath $chrome -ArgumentList $arguments
    Write-Host "Seat $seat  $directory"
}
