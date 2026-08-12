<div align="center">

# 🖥️ DynamicNotch

### A macOS Dynamic Island-style overlay for Windows

![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue?style=flat-square)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0%20WPF-purple?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)
![Status](https://img.shields.io/badge/Status-Active%20Development-orange?style=flat-square)

<br/>

<img src="https://img.shields.io/badge/Built%20with-❤️%20by%20Sanjana-ff69b4?style=for-the-badge" alt="Built with love by Sanjana"/>

<br/><br/>

**DynamicNotch** brings the beloved macOS Dynamic Island experience to Windows laptops and desktops.
A sleek, always-on-top pill-shaped overlay sits at the top-center of your screen, expanding on hover
to reveal media controls, a live calendar, weather, battery status, camera mirror, and more.

<br/>

</div>

---
╲ ╱
╲___ 🎵 [Album Art] 11:37 AM ▮▮▮ ___╱

text


### Expanded State
╲ ╱
╲____________________________________________________________________________╱
│ │
│ 🎵 Album Song Title Aug S M T W T F S 29°C 🔋100% ⚙ │
│ Art Artist Name 09 10 11 ⓬ 13 14 15 │
│ ⏮ ▶ ⏭ 📅 Intl. Youth Day 📷 Mirror │
│ │
╰────────────────────────────────────────────────────────────────────────────╯

text


---

## ✨ Features

### 🎵 Media Controls
- **Album art** displayed in both collapsed (32×26) and expanded (90×100) views
- **Song title** (14pt SemiBold) and **artist name** (11pt grey) with text trimming
- **Previous / Play-Pause / Next** buttons (28×28 each)
- **Animated equalizer bars** — 3 bars pulse with SineEase when music is playing
- Works with **any media app** — Spotify, YouTube, VLC, Windows Media Player, etc.
- Powered by Windows.Media.Control (SystemMediaTransportControls)

### 📅 Calendar & Events
- **7-day strip** centered on today (−3 to +3 days)
- **Day-of-week letters** (S M T W T F S) above date numbers
- **Today highlighted** with blue circle (#FF007AFF)
- **Weekends in red** (#FFFF453A)
- **Month label** displayed large (23pt Bold, e.g., "Aug")
- **Real-time events** powered by:
  - **Nager.Date API** — fetches country-specific holidays (IN, US, GB) including Diwali, Holi, Eid, Christmas, Easter, Ganesh Chaturthi, Ram Navami, Dussehra, Good Friday, and more
  - **150+ built-in international days** — Republic Day, Independence Day, World Youth Day, Earth Day, World Environment Day, International Women's Day, etc.
  - Multiple events on the same day are merged with " / "
  - Fallback text: 📅 No events today
  - Auto-caches per year, refreshes every 6 hours

### 🌤️ Weather
- **Open-Meteo API** (no API key required)
- **Celsius** temperature display
- **Auto-location** via ip-api.com geolocation (syncs to your actual city)
- Fallback location: Bengaluru, India (12.9716°N, 77.5946°E)
- Displays: Temperature, Condition, Icon, City, FeelsLike, Humidity
- Refreshes every **15 minutes**

### 🔋 Battery Indicator
- **10-second polling** via `System.Windows.Forms.SystemInformation.PowerStatus`
- **Color-coded**:
  - 🟢 Green: Above 30%
  - 🟠 Orange: 15–30%
  - 🔴 Red: ≤15%
- **Charging detection** with bolt (⚡) icons
- **Auto-hidden** on desktop PCs with no battery
- Icons: Segoe MDL2 Assets range `\uEBA5`–`\uEBAA` (normal) and `\uEBB0`–`\uEBB5` (charging)

### 📷 Camera Mirror
- **66×66 perfect circle** webcam preview via `Ellipse` clipping
- Horizontally mirrored (natural mirror effect) via `ScaleTransform ScaleX="-1"`
- Toggle on/off with the mirror button
- Camera icon shown when inactive (custom-drawn webcam icon)
- Requires Windows Privacy Settings → "Allow desktop apps to access camera"
- When mirror is active, the notch **never auto-collapses**

### ⏰ Clock
- **12-hour format** ("h:mm tt" → e.g., "11:37 AM")
- **12pt Segoe UI SemiBold**, centered in collapsed view
- Updates in real-time

### ⌨️ Global Hotkey
- **Ctrl+Shift+N** — toggles notch visibility from ANY app
- Registered via `RegisterHotKey` on a dedicated **STA background thread** with message loop
- Works in fullscreen games, browsers, and any application
- Animates slide-up/slide-down on toggle

### ⚙️ Settings
- Compact **240×120** floating panel
- Appears **to the right** of the expanded notch (aligned near gear button)
- Contains: "Settings" header, close ✕ button, "Quit DynamicNotch" button
- **Topmost** so it stays above other windows
- Draggable via `DragMove()`

### 🎬 Onboarding (First-Run Welcome)
- **720×640** window with pink-purple gradient background (NotchNook-inspired)
- **Custom app icon** — smiley notch face with mini notch on top
- **Handwritten-style notes** on both sides ("Thank you for using DynamicNotch :)" / "With ❤ From Sanjana")
- **3 feature preview cards** — mini notch previews showing Calendar, Media, and Weather/Battery
- **"Damn, that's awesome!"** blue CTA button with drop shadow glow
- macOS-style **close dot** (top-left)
- Fully draggable window
- **Shows only on first launch** — `IsFirstRun` flag saved via `SettingsService`

### 🎭 Concave Corner Hooks (macOS Notch Effect)
- Two **concave (inward-curving) Path shapes** on the top corners
- Creates the illusion that the notch is **carved out of the screen's top edge**
- Uses `PathGeometry` with `ArcSegment` for smooth curves
- Bottom corners have **CornerRadius 22** for rounded pill look

---

## 🎨 Design System

### Colors
| Token | Hex | Usage |
|-------|-----|-------|
| Background | `#FF000000` | Notch body |
| Accent Blue | `#FF007AFF` | Today circle, CTA buttons |
| Accent Green | `#FF30D158` | Playing state, battery >30% |
| Accent Orange | `#FFFF9F0A` | Battery 15-30% |
| Accent Red | `#FFFF453A` | Weekends, battery ≤15% |
| Text Primary | `White` | Main text |
| Text Secondary | `#FF8E8E93` | Subtitles, artist name |
| Text Tertiary | `#FF636366` | Event icons, muted text |

### Typography
| Element | Font | Size | Weight |
|---------|------|------|--------|
| Clock (collapsed) | Segoe UI | 12pt | SemiBold |
| Song title | Segoe UI | 14pt | SemiBold |
| Artist name | Segoe UI | 11pt | Regular |
| Month label | Segoe UI | 23pt | Bold |
| Day letters | Segoe UI | 10pt | Regular |
| Day numbers | Segoe UI | 12pt | Regular |
| Weather temp | Segoe UI | 12pt | SemiBold |
| Battery text | Segoe UI | 12pt | SemiBold |
| Icons | Segoe MDL2 Assets | Various | — |

### Dimensions
| Element | Value |
|---------|-------|
| Collapsed Width | 220px |
| Collapsed Height | 40px |
| Expanded Width | 680px |
| Expanded Height | 140px |
| Window Width (with hooks) | 248px |
| Window Height | 54px |
| Corner Radius (bottom) | 22px |
| Concave Hook Size | 14×14px |
| Top Offset | 0 (hooks touch screen edge) |
| Album Art (collapsed) | 32×26px |
| Album Art (expanded) | 90×100px |
| Mirror Button | 66×66px circle |
| Media Buttons | 28×28px each |
| Equalizer Bars | 2.5px wide, 4-14px height |

---

## ⚡ Animations

### Expand (Hover In)
| Property | From | To | Duration | Easing |
|----------|------|----|----------|--------|
| Width | 220 | 680 | 450ms | BackEase (0.15) |
| Height | 40 | 140 | 420ms | BackEase (0.08) |
| Collapsed Opacity | 1 | 0 | 100ms | CubicEase In |
| Expanded Opacity | 0 | 1 | 250ms (180ms delay) | CubicEase Out |

### Collapse (Hover Out)
| Property | From | To | Duration | Easing |
|----------|------|----|----------|--------|
| Expanded Opacity | 1 | 0 | 120ms | CubicEase In |
| Width | 680 | 220 | 380ms | ElasticEase (Springiness 8) |
| Height | 140 | 40 | 350ms | ElasticEase (Springiness 9) |
| Collapsed Opacity | 0 | 1 | 150ms (220ms delay) | CubicEase Out |

### Equalizer Bars
| Bar | From | To | Duration | Delay |
|-----|------|----|----------|-------|
| Bar 1 | 4px | 14px | 500ms | 0ms |
| Bar 2 | 14px | 4px | 600ms | 150ms |
| Bar 3 | 6px | 12px | 450ms | 300ms |
All use **SineEase InOut**, **AutoReverse**, **RepeatBehavior="Forever"**

### Timing
| Event | Delay |
|-------|-------|
| Hover → Expand | 120ms |
| Mouse Leave → Collapse | 800ms |
| Hover check polling | 300ms |
| Topmost guardian | 3 seconds |

---

## 🏗️ Architecture

### Tech Stack
- **Framework**: .NET 8.0 WPF
- **Target**: `net8.0-windows10.0.22621.0`
- **Minimum OS**: Windows 10 Build 19041
- **UI**: XAML with code-behind + MVVM (CommunityToolkit.Mvvm)
- **Language**: C# 12
- **IDE**: VS Code with C# Dev Kit

### Project Structure
DynamicNotch/
├─ DynamicNotch.csproj
├─ App.xaml
├─ App.xaml.cs
├─ Assets/
│ └─ placeholder-cover.png
├─ Models/
│ ├─ AppSettings.cs # Settings model with IsFirstRun flag
│ ├─ MediaState.cs # Media session state model
│ └─ CalendarDay.cs # Day cell model (Day, IsToday, IsWeekend, etc.)
├─ Services/
│ ├─ SettingsService.cs # JSON settings persistence
│ ├─ MediaSessionService.cs # Windows media session integration
│ ├─ StartupService.cs # Run-at-startup registry management
│ ├─ WindowStyleHelper.cs # DWM border removal, MakeIslandWindow()
│ ├─ WeatherService.cs # Open-Meteo API, Celsius, IP-based location
│ ├─ FullscreenDetectorService.cs# Fullscreen auto-hide (currently DISABLED)
│ ├─ SpringAnimator.cs # Custom spring animation helper
│ ├─ GlobalHotkeyService.cs # Ctrl+Shift+N via RegisterHotKey
│ ├─ BatteryService.cs # 10-second battery polling
│ └─ EventsService.cs # Nager.Date API + 150+ built-in events
├─ ViewModels/
│ └─ IslandViewModel.cs # Main ViewModel with all bindings
├─ Views/
│ ├─ IslandWindow.xaml # Main notch window (collapsed + expanded)
│ ├─ IslandWindow.xaml.cs # Code-behind with services + animations
│ ├─ OnboardingWindow.xaml # First-run welcome page
│ ├─ OnboardingWindow.xaml.cs # Onboarding code-behind
│ ├─ SettingsWindow.xaml # Compact settings panel
│ └─ SettingsWindow.xaml.cs # Settings code-behind
└─ .vscode/
└─ settings.json # VS Code workspace config

text


### NuGet Packages
| Package | Version | Purpose |
|---------|---------|---------|
| CommunityToolkit.Mvvm | 8.2.2 | MVVM toolkit (ObservableObject, RelayCommand) |
| Hardcodet.NotifyIcon.Wpf | 1.1.0 | System tray support (available but not actively used) |

### Key Services

| Service | Refresh Rate | Description |
|---------|-------------|-------------|
| `MediaSessionService` | Real-time | Hooks into Windows media transport controls |
| `WeatherService` | 15 min | Fetches weather from Open-Meteo |
| `BatteryService` | 10 sec | Polls battery status via WinForms API |
| `EventsService` | 6 hours | Fetches holidays + built-in international days |
| `GlobalHotkeyService` | Always-on | Listens for Ctrl+Shift+N on background thread |
| `FullscreenDetectorService` | — | DISABLED (can be re-enabled) |

### Window Behavior
- **Always on top** — enforced by topmost guardian (every 3 seconds via `SetWindowPos HWND_TOPMOST`)
- **Blocks minimize** — `OnStateChanged` override prevents Win+D from hiding it
- **No taskbar icon** — `ShowInTaskbar="False"`
- **No DWM border** — removed via `DWMWA_BORDER_COLOR = DWMWA_COLOR_NONE`
- **Transparent window** — `AllowsTransparency="True"`, `Background="Transparent"`
- **Hover detection** — 300ms polling via Win32 `GetCursorPos` (not WPF mouse events)

---

## 🚀 Getting Started

### Prerequisites
- **Windows 10** (Build 19041+) or **Windows 11**
- **.NET 8.0 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **VS Code** with [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) (recommended)
  - Or Visual Studio 2022 with .NET Desktop Development workload

### Clone & Build
powershell
# Clone the repository
git clone https://github.com/yourusername/DynamicNotch.git
cd DynamicNotch

# Build
dotnet clean
dotnet build

# Run
dotnet run
First Launch
On first run, the Onboarding window appears with a welcome message and feature overview
Click "Damn, that's awesome!" to launch the notch
The notch appears at the top-center of your primary screen
Hover over it to expand and see all features
Press Ctrl+Shift+N from anywhere to toggle visibility
Publish as Standalone .exe
PowerShell

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
Output: bin/Release/net8.0-windows10.0.22621.0/win-x64/publish/DynamicNotch.exe

🛠️ Configuration
.vscode/settings.json
JSON

{
    "dotnet.autoRestore.enabled": false,
    "dotnet.backgroundAnalysis.analyzerDiagnosticsScope": "openFiles",
    "dotnet.backgroundAnalysis.compilerDiagnosticsScope": "openFiles",
    "omnisharp.enableMsBuildLoadProjectsOnDemand": true,
    "files.exclude": {
        "**/bin": true,
        "**/obj": true,
        "**/*_wpftmp.csproj": true
    },
    "files.watcherExclude": {
        "**/bin/**": true,
        "**/obj/**": true,
        "**/*_wpftmp.*": true
    }
}
DynamicNotch.csproj
XML

<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <SupportedOSPlatformVersion>10.0.19041.0</SupportedOSPlatformVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <RootNamespace>DynamicNotch</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
    <PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="1.1.0" />
  </ItemGroup>
</Project>
      
🐛 Known Issues & Fixes

Issue	Fix
_wpftmp.csproj file lock errors	Don't run dotnet build while VS Code Dev Kit is auto-building. Close VS Code, kill dotnet.exe, delete temp files, rebuild
Webcam not showing	Enable "Allow desktop apps to access camera" in Windows Settings → Privacy → Camera
Notch disappears behind windows	Topmost guardian runs every 3s to re-enforce; if still hidden, press Ctrl+Shift+N twice
Grey DWM border around window	Fixed via DWMWA_BORDER_COLOR = DWMWA_COLOR_NONE in WindowStyleHelper
Multiple root elements in App.xaml	Only one <Application.Resources> block allowed
Application ambiguous reference	Use System.Windows.Application.Current.Shutdown() (not bare Application)
VideoMediaFrame not IDisposable	Use var instead of using var
Hotkey only works on desktop	Fixed by using dedicated STA background thread with hWnd = IntPtr.Zero
Troubleshooting Build Errors
PowerShell

# Nuclear clean
taskkill /F /IM DynamicNotch.exe 2>$null
taskkill /F /IM dotnet.exe 2>$null
Remove-Item -Recurse -Force bin, obj
Remove-Item *_wpftmp.* 2>$null

# Close VS Code completely, then:
dotnet clean
dotnet build
dotnet run

📋 Feature Status

Feature	Status
Collapsed pill (220×40)	✅ Working
Expanded island (680×140)	✅ Working
Spring animations (expand/collapse)	✅ Working
Media controls (play/pause/next/prev)	✅ Working
Album art (collapsed + expanded)	✅ Working
Animated equalizer bars	✅ Working
Calendar 7-day strip	✅ Working
Real-time events (Nager.Date + built-in)	✅ Working
Weather (Open-Meteo, Celsius)	✅ Working
Battery indicator (color-coded)	✅ Working
Camera mirror (66px circle)	✅ Working
Global hotkey (Ctrl+Shift+N)	✅ Working
Settings window	✅ Working
Onboarding (first-run)	✅ Working
Concave corner hooks	✅ Working
Topmost enforcement	✅ Working
Win+D block	✅ Working
Fullscreen auto-hide	❌ Disabled (can be re-enabled)
Multi-monitor support	❌ Not yet
System tray icon	❌ Removed

🗺️ Roadmap

 Glassmorphism/blur effect for the notch background
 Album art color extraction — dynamic accent colors from media artwork
 Volume slider in expanded media section
 Notification toasts — brief pop-up alerts
 Network speed monitor widget
 Multi-monitor support — notch on each display
 Theme system — light mode, dark mode, custom accent colors
 System tray icon — quick access menu
 Customizable hotkey — change from Ctrl+Shift+N to anything
 Re-enable fullscreen auto-hide with user toggle
 Auto-updater — check for new versions
 Plugin system — user-defined widgets
 
🤝 Contributing

Fork the repository
Create a feature branch: git checkout -b feature/awesome-widget
Commit your changes: git commit -m "Add awesome widget"
Push to the branch: git push origin feature/awesome-widget
Open a Pull Request
Development Guidelines
Provide complete file contents in PRs (not diffs) for clarity
Follow the existing color system and design constants
Test on both Windows 10 and Windows 11
Ensure the notch doesn't interfere with fullscreen apps or game overlays

📜 License

This project is licensed under the MIT License — see the LICENSE file for details.

🙏 Acknowledgments

NotchNook (macOS) — UI/UX inspiration for the onboarding page design
Apple Dynamic Island — original concept inspiration
Open-Meteo — free weather API (no key required)
Nager.Date — public holidays API
ip-api.com — IP geolocation service
CommunityToolkit.Mvvm — MVVM framework for WPF
Segoe MDL2 Assets — Microsoft's icon font for Windows
<div align="center">


DynamicNotch v1.0 — Your macOS-style notch, reimagined for Windows

</div> 
🚀 Save It
PowerShell

cd C:\Users\Mahalakshmi\Downloads\Sanjana\DynamicNotch
Create the file README.md in the root of your project folder and paste the entire content above.

📊 What's Included

Section	Contents
Header	Badges, tagline, description
Screenshots	ASCII art of collapsed/expanded states
Features	10 detailed feature sections (media, calendar, weather, battery, mirror, clock, hotkey, settings, onboarding, concave corners)
Design System	Colors, typography, dimensions tables
Animations	Expand, collapse, equalizer timing tables
Architecture	Tech stack, project structure, NuGet packages, services, window behavior
Getting Started	Prerequisites, build/run commands, first launch guide, publish command
Configuration	VS Code settings, csproj contents
Known Issues	Bug table + troubleshooting commands
Feature Status	Complete checklist
Roadmap	Future features
Contributing	Guidelines
License	MIT
Acknowledgments	Credits

## 📸 Screenshots

### Collapsed State (Pill)
