# Panduan Desain & Arsitektur Visual V-Notch (desain.md)

Dokumen ini disusun sebagai panduan komprehensif bagi pengembang untuk memahami, menyesuaikan, dan mengotak-atik antarmuka pengguna (UI/UX), tipografi, palet warna, dimensi geometri, dan animasi pada aplikasi **V-Notch (Zerono-tch-s)** berdasarkan standar desain asli repositori upstream `rainaku/V-Notch`.

---

## 1. Sistem Tipografi & Font (Typography)

Seluruh tipografi aplikasi diatur menggunakan class helper `fonts:AppFont` (`Services/AppFont.cs`) yang memuat font keluarga **SF Pro Display** secara tersemat (*embedded*), dengan fallback bertingkat otomatis ke font sistem Windows jika diperlukan.

### A. Definisi Resource Font Global (`App.xaml` & `MainWindow.xaml`)
| Resource Key | Definisi XAML | Fallback Font | Lokasi |
| :--- | :--- | :--- | :--- |
| `SFProDisplay` | `<fonts:AppFont Fallbacks="Segoe UI Emoji, Segoe UI, Arial"/>` | Segoe UI, Arial | `App.xaml` |
| `SFProText` | `<fonts:AppFont Fallbacks="Segoe UI Emoji, Segoe UI, Arial"/>` | Segoe UI, Arial | `App.xaml` |
| `MainSystemFont` | `<fonts:AppFont Fallbacks="Nirmala UI, Segoe UI"/>` | Nirmala UI, Segoe UI | `MainWindow.xaml` |
| `SettingsFont` | `<fonts:AppFont Fallbacks="Nirmala UI, Segoe UI"/>` | Nirmala UI, Segoe UI | `SettingsWindow.xaml` |
| `IconFont` | `pack://application:,,,/Fonts/#Segoe MDL2 Assets, Segoe Fluent Icons, ...` | Segoe UI Symbol | `App.xaml` |

### B. Ukuran Font & Ketebalan Standar di Elemen Utama
| Komponen UI | Ukuran (`FontSize`) | Ketebalan (`FontWeight`) | Style / Target |
| :--- | :--- | :--- | :--- |
| **Judul Lagu (Expanded)** | `13pt` / `14pt` | `Bold` | `TitleText` di `MainWindow.xaml` |
| **Nama Artis (Expanded)** | `11.5pt` | `Normal` / `Medium` | `SubtitleText` di `MainWindow.xaml` |
| **Lirik Lagu (Compact Notch)** | `11.5pt` | `SemiBold` | `CompactLyricText` (`MainWindow.xaml`) |
| **Fallback Judul (Compact)** | `11pt` | `Medium` | `CompactTrackFallbackText` |
| **Jam Digital (Standby)** | `12.5pt` | `SemiBold` | `StandbyClockText` |
| **Persentase Baterai** | `11.5pt` | `SemiBold` | `BatteryPercent` |
| **Persentase Volume OSD** | `11pt` | `SemiBold` (W=42, Right) | `CompactVolumePercentText` |
| **Menu Klik Kanan (ContextMenu)**| `13pt` | `Bold` | `TrayMenuItemText` |

---

## 2. Palet Warna & Brush (Color System)

Sistem warna diatur di `Services/UiPalette.cs` dan resource dictionary `MainWindow.xaml`:

### A. Palet Tinta & Teks (`UiPalette.cs`)
- **`PrimaryColor`**: `#FFFFFFFF` (Putih murni 100% opacity) — Digunakan untuk judul aktif, teks utama, dan ikon fokus.
- **`SecondaryColor`**: `#B3FFFFFF` (Putih 70% opacity / `0xB3`) — Digunakan untuk artis, subtitle, dan durasi sekunder.
- **`TertiaryColor`**: `#73FFFFFF` (Putih 45% opacity / `0x73`) — Digunakan untuk ikon pasif, border samar, dan placeholder.

### B. Warna Latar Belakang & Gradien (`MainWindow.xaml`)
- **`NotchGradient`**:
  ```xml
  <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
      <GradientStop Color="#FF050505" Offset="0"/>
      <GradientStop Color="#FF000000" Offset="1"/>
  </LinearGradientBrush>
  ```
  Warna hitam pekat OLED dengan sedikit sentuhan abu-abu sangat gelap di bagian atas.
- **`CardGradient` (MediaWidget)**:
  ```xml
  <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
      <GradientStop Color="#12FFFFFF" Offset="0"/>
      <GradientStop Color="#0AFFFFFF" Offset="1"/>
  </LinearGradientBrush>
  ```
- **Sekat Garis Pemisah (Divider)**: `#1AFFFFFF` (Putih 10% opacity tipis).
- **Badge Mute Mic**: `#E5FF3B30` (Merah iOS/macOS pekat).
- **Badge Copied (Green Check)**:
  - Background: `#2622C55E` (Hijau 15% opacity).
  - Border: `#4D22C55E` (Hijau 30% opacity).
  - Glyph/Checkmark: `#22C55E` (Hijau emerald terang).
- **Privacy Dot (Mic/Cam In-Use)**:
  - Mikrofon: `#FF9500` (Oranye).
  - Kamera: `#30D158` (Hijau neon).
  - Screen Recording: `#FF3B30` (Merah).

---

## 3. Dimensi, Geometri & Layout (Dimensions)

Semua dimensi dasar diatur dinamis melalui `_settings` dan disinkronkan via `MainWindow.AnimationHelpers.cs`:

### A. Mode Compact (Notch Tertutup)
| Mode | Lebar Standar (`Width`) | Tinggi Standar (`Height`) | Lengkungan (`CornerRadius`) |
| :--- | :--- | :--- | :--- |
| **Notch Mode (Standar)** | `_settings.Width` (Default: `230px`) | `_settings.Height` (Default: `34px`) | `_settings.CornerRadius` (Default: `8px`) |
| **Dynamic Island Mode** | `_settings.DynamicIslandWidth` (`220px`) | `_settings.DynamicIslandHeight` (`40px`)| `Height / 2.0` (Kapsul oval sempurna) |
| **Mode Standby (Jam)** | `_collapsedWidth` | `_collapsedHeight` | Mengikuti mode aktif |
| **Mode Lirik Aktif** | Dinamis: `Math.Max(_collapsedWidth, LirikW + 90)` | `_collapsedHeight` | Mengikuti mode aktif |
| **Volume OSD** | Tetap: `280px` | `_collapsedHeight` | Mengikuti mode aktif |
| **Copied OSD** | Dinamis: `Math.Clamp(TextW + 110, 200, 340)` | `_collapsedHeight` | Mengikuti mode aktif |

### B. Mode Expanded (Notch Terbuka)
- **Expanded Width**: Dihitung otomatis `Math.Max(380, SystemParameters.WorkArea.Width * 0.28)`.
- **Expanded Height**: `147px` (Notch Mode) atau `154px` (Dynamic Island Mode).
- **MediaWidget Padding**: `12,12,6,4` (Proporsi asli agar ruang slider durasi tidak terhimpit).
- **Thumbnail Album Art**: `102x102 px` dengan `CornerRadius="14"` dan margin kanan `8px`.
- **Thumbnail Mini Compact**: `24x24 px` dengan `CornerRadius="6"` dan margin kanan `10px`.
- **Ikon OSD Mini**: `24x24 px` dengan `CornerRadius="6"` dan margin kanan `10px`.
- **Center Alignment**: Seluruh elemen compact notch diapit di tengah dengan `VerticalAlignment="Center"` dan `HorizontalAlignment="Center"`, dengan `Margin="0"` pada sumbu vertikal untuk simetri absolut atas-bawah.

---

## 4. Sistem Animasi & Transisi (Animation Primitives)

Semua animasi diatur oleh `Services/AnimationPrimitives.cs` dan berjalan di atas kompositor WPF dengan target refresh rate monitor (`TargetFps` hingga 120/144/240Hz):

### A. Durasi Baku (TimeSpan Constants di `MainWindow`)
- **`_dur100`**: `100ms` — Transisi instan visualizer & ikon kecil.
- **`_dur150`**: `150ms` — Fade out thumbnail saat pergantian track.
- **`_dur200`**: `200ms` — Fade in lirik baru & crossfade teks.
- **`_dur250`**: `250ms` — Animasi snap slider progress volume.
- **`_dur350`**: `350ms` — Perubahan lebar kapsul notch (`WidthProperty`).
- **`_dur400`**: `400ms` — Animasi slide buka/tutup Expanded View.
- **`_dur500`**: `500ms` — Transisi bentuk besar (CollapseAll / ExpandNotch).

### B. Easing Functions Pilihan
- **`_easeExpOut6` (`ExponentialEase Exponent=6`)**: Digunakan untuk pergerakan lebar kapsul notch agar terasa responsif di awal dan melambat lembut di akhir.
- **`_easeQuadOut` (`QuadraticEase EaseOut`)**: Digunakan untuk opasitas (fade in / fade out).
- **`_cubicEaseOut` (`CubicEase EaseOut`)**: Digunakan untuk transisi ukuran OSD.
- **`_easeSoftSpring`**: Digunakan untuk efek kenyal Dynamic Island saat disentuh.

---

## 5. Peta Struktur Elemen XAML (`x:Name` Cheat Sheet)

Gunakan tabel ini jika ingin mengubah atau memodifikasi letak elemen di `Windows/MainWindow.xaml`:

| `x:Name` | Tipe Kontrol | Fungsi & Peran | Letak di `MainWindow.xaml` |
| :--- | :--- | :--- | :--- |
| `NotchWrapper` | `Grid` | Kontainer utama notch terluar (Event Mouse, Drag, ContextMenu) | Baris ~480 |
| `NotchBorder` | `Border` | Kapsul visual utama yang dianimasikan Width, Height, CornerRadius | Baris ~520 |
| `CollapsedContent` | `Grid` | Wadah Mode Standby (Jam Digital `HH:mm` dan badge mic standby) | Baris ~1025 |
| `StandbyClockText` | `TextBlock` | Teks jam digital saat notch tidak memutar media | Baris ~1030 |
| `StandbyMicBadge` | `ActionBorder` | Ikon merah 🔇 di sebelah jam standby saat mic muted | Baris ~1037 |
| `MusicCompactContent`| `Grid` | Kontainer utama Mode Compact Musik (Grid 4 Kolom) | Baris ~1317 |
| `CompactThumbnailBorder`| `Border` | Kotak album art mini 26x26 di sisi kiri compact notch | Baris ~1334 |
| `CompactThumbnail` | `Image` | Gambar cover album lagu saat ini | Baris ~1362 |
| `CompactActionIconBorder`| `Border` | Kotak ikon aksi OSD di Kolom 0 (Speaker / Mic / Chat) | Baris ~1450 |
| `CompactCenterHost`| `Grid` | Area teks utama Kolom 1 (*ClipToBounds* anti-tabrak) | Baris ~1468 |
| `CompactLyricsHost` | `Grid` | Wadah lirik tersinkronisasi dan fallback judul | Baris ~1476 |
| `CompactLyricText` | `TextBlock` | Teks lirik lagu yang sedang menyanyi real-time | Baris ~1479 |
| `CompactTrackFallbackText`| `TextBlock` | Teks `Judul • Artis` saat intro/instrumental tanpa lirik | Baris ~1494 |
| `CompactVolumeOsd` | `Grid` | Wadah OSD Volume (Progress bar slider + persentase) | Baris ~1512 |
| `CompactVolumeFill` | `Border` | Bilah warna pengisi volume audio real-time | Baris ~1526 |
| `CompactVolumePercentText`| `TextBlock` | Angka persentase volume (`0%` - `100%`, lebar tetap 42px) | Baris ~1532 |
| `CompactActionText`| `TextBlock` | Teks alert status transient (misal "Microphone Muted") | Baris ~1543 |
| `CompactCopiedHost`| `Grid` | Wadah OSD "Copied" 3 Kolom (Badge hijau, cuplikan teks) | Baris ~1560 |
| `CompactCopiedSnippet`| `TextBlock`| Cuplikan teks clipboard dengan tanda kutip (max 28 char) | Baris ~1600 |
| `CompactStatusPanel`| `StackPanel`| Kolom 2 khusus status indicator (bebas tabrakan) | Baris ~1625 |
| `MicMutedBadge` | `ActionBorder` | Badge lingkaran merah 20x20 🔇 interaktif klik-to-unmute | Baris ~1630 |
| `MusicViz` | `MusicVisualizer`| Waveform gelombang suara batang di Kolom 3 | Baris ~1645 |
| `NavIconsPanel` | `Grid` | Bar navigasi atas Expanded View (Home, Shelf, Timer, Audio) | Baris ~850 |
| `StatusBar` | `Grid` | Bar pojok kanan atas Expanded View (Baterai & Gear Settings) | Baris ~1706 |
| `BatterySection` | `ActionBorder` | Tampilan persentase & ikon baterai laptop | Baris ~1719 |
| `SettingsButton` | `ActionBorder` | Tombol roda gigi ⚙ untuk membuka jendela Settings | Baris ~1846 |
| `MediaWidget` | `Border` | Panel kontrol media player di Expanded View | Baris ~2012 |
| `ThumbnailBorder` | `Border` | Kotak cover album art besar (102x102) di Expanded View | Baris ~2031 |

---

## 6. Tips Mengotak-Atik Antarmuka (Customization Guide)

1. **Mengubah Kecepatan Animasi Buka-Tutup:**
   Buka `Windows/MainWindow.Animation.cs` lalu cari `_dur350` atau `_dur500`. Naikkan milidetik untuk animasi yang lebih santai (*cinematic*), atau turunkan ke `250ms` untuk respon yang sangat cepat.
2. **Mengubah Jarak Spasi Teks Lirik:**
   Buka `Windows/MainWindow.xaml` pada elemen `CompactCenterHost` (Baris ~1468) lalu sesuaikan atribut `Margin="0,0,12,0"`.
3. **Mengatur Ukuran Dasar Notch:**
   Cukup buka menu **Settings** (`SettingsWindow`) melalui klik kanan notch $\rightarrow$ atur slider `Width`, `Height`, atau `Dynamic Island Length/Width` $\rightarrow$ klik **Apply** atau **Save**. Perubahan langsung berlaku secara real-time!
