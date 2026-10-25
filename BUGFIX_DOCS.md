# V-Notch Architecture & Bugfix Documentation

Dokumentasi ini mencatat secara rinci riwayat bug kritis, akar penyebab (*root cause*), dan arsitektur solusi yang telah diterapkan pada repositori **V-Notch (Zerono-tch-s)**. Dokumen ini wajib dibaca sebelum melakukan perubahan kode di masa mendatang guna mencegah terjadinya regresi (*regression bugs*).

---

## 1. Startup Crash Fix (Delegate Type Mismatch & Cascading Teardown)
- **Gejala:** Aplikasi langsung force close saat startup dengan pop-up error:
  `System.Windows.Markup.XamlParseException: Failed to create a 'Click' from the text 'MicMutedBadge_Click'`.
  Diikuti oleh crash kedua: `System.NullReferenceException at MainWindow.PerformCleanup()`.
- **Akar Masalah (Root Cause):**
  1. Kontrol kustom `ActionBorder` di `Controls/ActionBorder.cs` mendefinisikan event `Click` bertipe `MouseButtonEventHandler` (`MouseButtonEventArgs`), bukan standard WPF button `RoutedEventHandler` (`RoutedEventArgs`).
  2. Di code-behind, method handler ditulis dengan signature `(object sender, RoutedEventArgs e)`, sehingga WPF XamlReader gagal mengikat (*bind*) delegasi dan melempar `ArgumentException`.
  3. Kegagalan konstruktor `InitializeComponent()` menghentikan instansiasi objek sebelum field service (`_moduleHost`, `_notchManager`, dll.) terisi. Ketika WPF memanggil `OnClosed` -> `PerformCleanup()`, pemanggilan tanpa null-check (`_moduleHost.StopAll()`) memicu `NullReferenceException`.
- **Solusi Kode:**
  - Ubah signature event handler menjadi `(object sender, MouseButtonEventArgs e)`.
  - Pasang null-conditional operator (`?.`) pada seluruh pembersihan resource di `PerformCleanup()`.
  - File terkait: `Windows/MainWindow.SystemAlerts.cs`, `Windows/MainWindow.Controls.cs`, `Windows/MainWindow.xaml.cs`.

---

## 2. Sistem 2 Ukuran Tetap: "Smart Standby Mode" vs "Fixed Active Mode"
- **Gejala:** Lebar Compact Notch berubah-ubah secara liar dan melompat-lompat mengikuti panjang baris lirik lagu atau teks OSD, menutupi tab browser di atas layar.
- **Arsitektur Solusi:**
  Mengganti kalkulasi lebar dinamis dengan **2 ukuran tetap** yang bertransisi halus via `DoubleAnimation` (`CubicEase` EaseOut, `300ms`):
  1. **Mode Standby / Idle (`130x32 px`):**
     - Aktif saat tidak ada media yang berputar dan tidak ada OSD aksi yang aktif.
     - Menampilkan **Jam Digital (`HH:mm`)** dengan font rapi di tengah (`StandbyClockText`), serta menampilkan badge kecil Mic-Off merah (`StandbyMicBadge`) jika mic sedang di-mute.
  2. **Mode Active (`360x36 px`):**
     - Aktif saat ada lagu/video berputar ATAU saat ada aksi sementara (Volume, Mic Alert, Copied, Toast Chat).
     - Ukuran notch **terkunci konsisten pada 360x36 px** tanpa resizing liar. Pergantian antar-konten di dalamnya berjalan mulus menggunakan animasi fade.
  - File terkait: `Windows/MainWindow.CompactPill.cs`, `Windows/MainWindow.AnimationHelpers.cs`, `Windows/MainWindow.xaml`.

---

## 3. Strict Mutual Exclusion & Timer Lock (`_isTransientOsdActive`)
- **Gejala:** Teks `"Copied"`, `"Microphone Muted"`, atau Bar Volume bertumpuk tepat di atas kalimat lirik lagu yang sedang berjalan.
- **Akar Masalah:**
  Timer lirik (`UpdateLyricsDisplay` / `LyricsTimer_Tick`) terus mengirim pembaruan teks lirik setiap 100ms dan menimpa tampilan OSD yang sedang aktif.
- **Solusi Kode:**
  1. **State Lock:** Memperkenalkan flag `_isTransientOsdActive = true` selama OSD sementara tampil (1.5–2 detik).
  2. **Timer Guard:** Pada awal `UpdateCompactLyricsDisplay` dan `UpdateLyricsDisplay`, tambahkan pengecekan:
     ```csharp
     if (_isTransientOsdActive) return;
     ```
  3. **Centralized Content Switcher (`SetCompactModeContent`):**
     Satu method terpusat yang mengatur visibilitas konten compact secara mutlak:
     - `VolumeOsd`: Hanya menampilkan ikon speaker dan volume slider + persentase. Menyembunyikan seluruh elemen lirik, thumbnail, waveform, dan OSD lain.
     - `MicAlertOsd`: Hanya menampilkan ikon mic dan teks alert status.
     - `CopiedOsd`: Hanya menampilkan teks "Copied" dan ikon checkmark.
     - `MediaLyrics`: Hanya menampilkan cover art, teks lirik/judul, waveform, dan badge mute (jika mic muted).
  - File terkait: `Windows/MainWindow.CompactPill.cs`, `Windows/MainWindow.Lyrics.cs`.

---

## 4. Layout Grid Anti-Tabrak (Strict Columns Layout)
- **Gejala:** Huruf terakhir kalimat lirik lagu menabrak titik oranye atau badge mute mic di sebelah kanan.
- **Solusi Kode:**
  Menggunakan layout Grid 4 kolom pada `MusicCompactContent`:
  - `Column 0 (Auto)`: Thumbnail Album (`26x26`) atau Action Icon (`Margin="0,0,10,0"`).
  - `Column 1 (*)`: Area teks utama dengan `ClipToBounds="True"`, `Margin="0,0,12,0"`, dan `TextTrimming="CharacterEllipsis"`.
  - `Column 2 (Auto)`: Slot status indicator untuk badge mute mic (`20x20`, `Margin="0,0,8,0"`).
  - `Column 3 (Auto)`: Waveform visualizer audio atau checkmark feedback.
  - File terkait: `Windows/MainWindow.xaml`.

---

## 5. Pemisahan Logika Mic Mute (Alert 1.5 Detik vs Badge Permanen)
- **Gejala:** Saat mic di-mute, notch stuck terkunci di mode mute dan tidak mau kembali menampilkan lirik lagu.
- **Solusi Kode:**
  1. **Alert Sementara (1.5 detik):** Ditangani oleh sistem Prioritas 1 OSD (`CompactTransientAction.MicAlert`). Kolom 1 menampilkan teks `"Microphone Muted"` (`#FF6B6B`) atau `"Microphone Active"` (`#30D158`), lalu auto-dismiss setelah 1.5 detik.
  2. **Badge Permanen:** Ditempatkan di Kolom 2 (`MicMutedBadge`). Ikon 🔇 merah `20x20` menyala selama hardware mic masih mute tanpa mengganggu lirik atau thumbnail lagu di kolom lainnya.
  3. **PrivacyDot Suppression:** Ketika mic di-mute, titik oranye `PrivacyDot` otomatis disembunyikan (`ShouldSuppressPrivacyDot`) agar tidak bertumpuk di atas badge merah.
  - File terkait: `Windows/MainWindow.SystemAlerts.cs`, `Windows/MainWindow.PrivacyIndicators.cs`.

---

## 6. Zero-Click Auto-Resume & Stale Thumbnail Fix
- **Gejala:**
  1. Setelah OSD volume/copied/mute selesai, notch menjadi hitam kosong dan membutuhkan klik manual untuk memunculkan lirik kembali.
  2. Saat berpindah dari lagu Spotify ke video YouTube (contoh: video berita/game), thumbnail cover album sebelumnya masih nyangkut.
- **Solusi Kode:**
  1. **Auto-Resume:** Method `EvaluateAndRestoreNotchState()` dipanggil secara otomatis saat timer OSD selesai, mengevaluasi `_currentMediaInfo`, dan langsung memulihkan tampilan lirik / standby clock tanpa perlu interaksi pengguna.
  2. **Thumbnail Reset:** Pada `MainWindow.Media.cs`, saat `result.IsNewTrack` bernilai `true`:
     - Jika media baru belum/tidak memiliki thumbnail (`info.Thumbnail == null`), bersihkan gambar lama secara tuntas:
       ```csharp
       CompactThumbnail.Source = null;
       ThumbnailImage.Source = null;
       ThumbnailImage.Visibility = Visibility.Collapsed;
       ThumbnailFallback.Visibility = Visibility.Visible;
       ```
     - Hindari fallback ke thumbnail lama `?? ThumbnailImage.Source`.
  - File terkait: `Windows/MainWindow.CompactPill.cs`, `Windows/MainWindow.Media.cs`.

---

## 7. Auto-Hide Saat Fullscreen (YouTube / Game / Video)
- **Gejala:** Notch tetap melayang di atas layar saat pengguna sedang menonton video YouTube Fullscreen atau bermain game.
- **Akar Masalah:**
  Setelan `HideOnExclusiveFullscreen` dan `HideOnWindowedFullscreen` sebelumnya bernilai `false` secara default, dan timer polling dimatikan.
- **Solusi Kode:**
  1. Di `Models/NotchSettings.cs`, ubah default nilai menjadi `true`.
  2. Di `Controllers/FullscreenAutoHideController.cs`, jalankan `_pollTimer.Start()` dan pastikan `ShouldHideForType` mengembalikan `true` saat `FullscreenDetector` mendeteksi jendela foreground menutupi area layar utama.
  3. Saat fullscreen terdeteksi, notch otomatis meluncur keluar layar (`Top = -slideDistance`, `Opacity = 0`, `IsHitTestVisible = false`). Begitu pengguna menekan `Esc`, notch meluncur kembali secara halus.
  - File terkait: `Controllers/FullscreenAutoHideController.cs`, `Models/NotchSettings.cs`, `Windows/MainWindow.xaml.cs`.

---

## 8. Pembersihan Expanded View Top-Right & Menu Klik Kanan Asli
- **Gejala:**
  1. Ikon roda gigi Settings di pojok kanan atas Expanded View tertimpa langsung oleh tombol silang (`✕`) sehingga tidak bisa diklik.
  2. Saat notch diklik kanan, hanya muncul kotak hitam kosong dengan 2 garis tanpa tulisan apa pun.
  3. Durasi total lagu di progress bar Expanded View terpotong menjadi `3:`.
- **Akar Masalah:**
  1. `NavIconsPanel` dan `StatusBar` sama-sama memiliki alignment di kanan atas Expanded View. Tambahan tombol silang di `NavIconsPanel` berada di koordinat yang persis menimpa `SettingsButton` di `StatusBar`.
  2. `ContextMenu` kustom menggunakan `Style="{StaticResource TrayContextMenuStyle}"` tanpa menyertakan `Style="{StaticResource TrayMenuItemStyle}"` dan `<TextBlock Style="{StaticResource TrayMenuItemText}"/>` pada `MenuItem`-nya, sehingga ControlTemplate tidak merender teks item.
  3. Padding `MediaWidget` yang terlalu besar (`16,14,16,14`) dan margin thumbnail (`14px`) menyempitkan ruang horizontal `ProgressSection`, sehingga `RemainingTimeText` di kolom kanan terpotong.
- **Solusi Kode:**
  1. Menghapus tombol silang (`✕`) dan tombol duplikat dari `NavIconsPanel` sehingga pojok kanan atas Expanded View bersih sesuai desain asli `rainaku/V-Notch` (hanya ada `BatterySection` dan `SettingsButton` ⚙).
  2. Mengembalikan padding `MediaWidget` ke proporsi asli (`12,12,6,4`) dan margin thumbnail ke `8px`, serta menyetel `RemainingTimeText` dengan `HorizontalAlignment="Right"` dan `TextAlignment="Right"` sehingga durasi total lagu (misal `3:45`) tampil utuh.
  3. Menulis ulang `ContextMenu` pada `NotchWrapper` menggunakan `TrayMenuItemStyle`, `TraySeparatorStyle`, dan `TrayMenuItemText` lengkap dengan ikon vektor bawaan, efek hover `#18FF453A`, dan 4 opsi fungsional:
     - `⚙ Open Settings`
     - `🔄 Refresh Notch State`
     - `👁 Hide Notch (Until Next Hover/Click)`
     - `🚪 Exit Application`
  - File terkait: `Windows/MainWindow.xaml`, `Controls/MediaPlaybackView.xaml`, `Windows/MainWindow.Controls.cs`.

---

## 9. Smooth Waveform Decay & High-Contrast Icons
- **Penyempurnaan:**
  1. **Waveform Lerp Decay:** Nilai `AlphaPauseRelease` pada `Controls/MusicVisualizer.cs` diubah menjadi `0.85` sehingga saat musik di-pause, batang visualizer turun perlahan dan lembut menuju garis titik-titik datar (`.....`) dalam kurun waktu ~300ms.
  2. **Icon Contrast:** Teks ikon di dalam `CompactActionIconBorder` diatur eksplisit dengan `Foreground="#FFFFFF"` dan teks alert mute menggunakan `#FF6B6B` agar kontras dan terbaca jelas di atas latar gelap.
  - File terkait: `Controls/MusicVisualizer.cs`, `Windows/MainWindow.xaml`, `Windows/MainWindow.SystemAlerts.cs`.

---

## 10. WinRT & UI Automation Notification Fallback
- **Penyempurnaan:**
  1. Mendukung deteksi WhatsApp Web dari browser (`Chrome`, `Edge`, `Brave`, `Firefox`) dengan memeriksa kecocokan nama, AppId, judul, serta isi pesan toast.
  2. Menyediakan fallback otomatis menggunakan Windows UI Automation (`Automation.AddAutomationEventHandler`) apabila aplikasi dijalankan sebagai unpackaged Win32 app yang tidak memiliki izin akses WinRT `UserNotificationListener`.
  - File terkait: `Services/SystemNotificationListenerService.cs`.

---

## 11. Perbaikan Aksesibilitas Tombol & Jendela Settings (Settings Window Activation)
- **Gejala:** Pengguna mengeluhkan tombol settings tidak bisa dibuka saat notch aktif di layar.
- **Akar Masalah:**
  1. Pada `SettingsWindow.Animation.cs`, kalkulasi `ShellTranslate.X/Y` mengambil nilai `Left` dan `Top` yang bernilai `double.NaN` sebelum jendela selesai di-arrange di layar. Nilai `NaN` pada transform menyebabkan elemen `MainShell` menghilang seketika.
  2. Event `Click` pada `ActionBorder` di status bar terhalang oleh event capture induknya, dan nilai default `Opacity` terinisialisasi `0`.
- **Solusi Kode:**
  1. Memasang penjaga `double.IsNaN` pada seluruh kalkulasi koordinat awal `PlayEntranceAnimation` di `SettingsWindow.Animation.cs`.
  2. Memasang binding `PreviewMouseLeftButtonDown="Settings_Click"` pada tombol settings dan memastikan default `Opacity = 1`.
  3. Memperkuat method `OpenAppSettings()` dengan blok try/catch, fallback instansiasi fresh `CreateSettingsWindow(false)`, penempatan `CenterScreen`, serta pemanggilan `Activate()` dan `Focus()`.
  - File terkait: `Windows/SettingsWindow.Animation.cs`, `Windows/MainWindow.xaml`, `Windows/MainWindow.xaml.cs`.

---

## 12. Desain Ulang OSD "Copied" Proporsional (3 Kolom & Dynamic Width 200–340px)
- **Gejala:** Tampilan OSD "Copied" sebelumnya terasa kosong melompong di dalam kapsul hitam 360px tanpa ada ikon clipboard atau checkmark.
- **Arsitektur Solusi:**
  1. **Layout 3 Kolom (`CompactCopiedHost`):**
     - Kolom Kiri: Badge aksen hijau lembut 24x24 (`#2622C55E` border `#4D22C55E`) dengan ikon checkmark bold (`✓`, `#22C55E`).
     - Kolom Tengah: Teks **`Copied`** (Bold `#FFFFFF`, 12.5pt) + ` • ` + cuplikan teks clipboard maksimal 25–28 karakter (`#B0B0B0` ellipsis), atau *"Image"* / *"Files"* jika non-teks.
      - Kolom Kanan: Ikon checkmark hijau status (`Margin="10,0,0,0"`).
   2. **Lebar Dinamis Proporsional:**
      - Lebar kapsul dihitung otomatis berdasarkan panjang teks cuplikan: `Math.Clamp(MeasureCompactTextWidth + 110, 200, 340)`.
      - Kapsul menyempit secara mulus (`DoubleAnimation` CubicEase EaseOut `250ms`) saat "Copied" muncul, dan melebar kembali saat lirik lagu kembali aktif.
   - File terkait: `Windows/MainWindow.xaml`, `Windows/MainWindow.ClipboardPeek.cs`, `Windows/MainWindow.CompactPill.cs`.

---

## 13. Perbaikan Bug Volume OSD Jitter / "Kedut-Kedut" (Debounce Animation & Fixed Layout)
- **Gejala:** Saat menaikkan atau menurunkan volume (terutama saat tombol volume keyboard ditahan atau digulir roda mouse), notch bergetar atau melompat-lompat (*jitter / shake*) secara liar.
- **Akar Masalah:**
  Setiap event perubahan volume memicu pemanggilan `SetTransientAction(CompactTransientAction.Volume)` yang meluncurkan `DoubleAnimation` baru pada `WidthProperty` dan `HeightProperty` kapsul notch dengan mode `SnapshotAndReplace`. Akibatnya puluhan animasi lebar bertabrakan per detik. Selain itu, lebar angka persentase volume yang berubah (misal dari `9%` ke `10%` atau `100%`) menggeser slider bar di sebelahnya.
- **Solusi Kode:**
  1. **Debounce Animation:** Di `MainWindow.CompactPill.cs`, periksa apakah Volume OSD sudah aktif (`wasAlreadyThisAction = _isTransientOsdActive && _activeTransientAction == action`). Jika sudah aktif, **dilarang keras memicu animasi ukuran kapsul baru!** Cukup perbarui posisi fill progress bar (`CompactVolumeFill.Width`) dan angka persentasenya, lalu restart timer dismiss 1.5 detik.
  2. **Fixed Layout 280px:** Kapsul Volume OSD diatur pada lebar tetap `280px`. TextBlock angka persentase diberikan lebar tetap `Width="42"` dan `TextAlignment="Right"` agar perubahan jumlah digit angka tidak mendorong posisi progress bar.
  - File terkait: `Windows/MainWindow.CompactPill.cs`, `Windows/MainWindow.Controls.cs`, `Windows/MainWindow.xaml`.

---

## 14. Perbaikan Keterlambatan Metadata & Staged Thumbnail Retry pada Browser SMTC
- **Gejala:** Ketika lagu baru mulai berputar di YouTube Music atau Spotify Web (misal dari playlist otomatis), judul lagu, lirik, dan cover album di notch terlambat berganti beberapa detik atau masih nyangkut di lagu sebelumnya.
- **Akar Masalah:**
  1. Background heartbeat `HeartbeatLoopAsync` sebelumnya berjalan sangat lambat (`3–5 detik`). Selain itu, saat heartbeat berjalan, flag `forceRefresh` bernilai `false` sehingga SMTC tidak me-rescan sesi aktif.
  2. Browser Chromium (Chrome/Edge/Brave) sering tidak memicu event `MediaPropertiesChanged` saat pergantian lagu otomatis, dan stream `Thumbnail` di SMTC Windows sering terlambat diisi oleh browser pada detik `0:00–0:02`.
  3. `SubscribeToCurrentSession()` hanya mengambil `sessionManager.GetCurrentSession()` yang sering menunjuk ke tab browser yang sedang pause, bukan tab YouTube Music yang sedang play.
- **Solusi Kode:**
  1. **Prioritaskan Sesi Playing:** Di `SubscribeToCurrentSession()`, periksa seluruh sesi di `GetSessions()` dan utamakan sesi dengan `PlaybackStatus == Playing`.
  2. **Fast Polling 500ms:** Ubah interval heartbeat loop menjadi `500ms` saat sesi aktif dan sertakan `ChangeTypes.Heartbeat` ke dalam `ForceRefreshMask` agar pergantian lagu terdeteksi dalam waktu maksimal 500ms.
  3. **Staged Thumbnail Retry:** Pada method `TrackNameChangeBookkeeping`, saat `info.CurrentTrack` berubah:
     - Langsung bersihkan thumbnail lama (`CachedThumbnail = null;`).
     - Jalankan `TriggerStagedTrackRefresh()` yang memicu refresh bertahap di background pada jeda **`300ms`, `800ms`, dan `1800ms`** untuk membaca ulang stream cover art SMTC yang baru masuk.
   4. **Instant UI Response:** Di `MainWindow.Media.cs`, panggil `EvaluateAndRestoreNotchState()` di akhir `ApplyMediaUpdate` agar perubahan judul dan cover langsung di-render ke Compact Notch tanpa menunggu hover/klik mouse.
   - File terkait: `Services/MediaDetectionService.cs`, `Windows/MainWindow.Media.cs`.

---

## 15. Perbaikan Foto Album Asli & Eliminasi Subtitle Mismatch YouTube Music
- **Gejala:**
  1. Saat memutar lagu di YouTube Music, foto album yang muncul di notch tidak sesuai dengan cover asli lagu (ditimpa oleh frame video hasil scraping YouTube).
  2. Subtitle yang muncul tidak cocok dengan lagu yang sedang berputar karena mengambil transkrip ucapan video YouTube acak.
- **Akar Masalah:**
  1. `ThumbnailHeuristics.cs` menolak (*reject*) thumbnail SMTC YouTube/Browser dan memicu `FetchYouTubeThumbnailAsync`, yang mengunduh thumbnail frame video 16:9 (`maxresdefault.jpg`) dari kueri pencarian video YouTube publik. Akibatnya cover album asli YouTube Music (persegi 256x256 atau 512x512) tertimpa oleh gambar video yang salah.
  2. Saat LRCLIB tidak menemukan lirik untuk lagu tertentu, `MainWindow.Lyrics.cs` jatuh ke fallback `FetchSubtitlesForTrack`, yang mengunduh closed-caption transkrip ucapan video YouTube otomatis alih-alih menampilkan lirik lagu asli.
- **Solusi Kode:**
  1. **Preserve Native SMTC Album Art:** Di `ThumbnailHeuristics.cs`, jika thumbnail SMTC berformat bujur sangkar (aspect ratio 1.0 $\pm$ 0.1) dan berukuran $\ge 120\text{px}$, tetapkan keputusan sebagai `Accept`. Di `MediaDetectionService.cs`, jika thumbnail SMTC bujur sangkar sudah tersedia, **jangan timpa dengan frame video YouTube eksternal**.
  2. **Eliminasi Subtitle Mismatch:** Pada `MainWindow.Lyrics.cs`, jika LRCLIB tidak menemukan lirik tersinkronisasi, tampilkan fallback bersih `Judul Lagu • Artis` tanpa mengunduh auto-generated video captions YouTube yang tidak cocok.
   3. **Tampilan Lirik Multi-Kalimat:** Pada `UpdateCompactLyricsDisplay`, izinkan lebar notch menyesuaikan dinamis hingga 600px mengikuti panjang baris lirik multi-kalimat yang sedang dinyanyikan.
   - File terkait: `Services/ThumbnailHeuristics.cs`, `Services/MediaDetectionService.cs`, `Windows/MainWindow.Lyrics.cs`.

---

## 16. Perbaikan Lirik Hilang Setelah Pause Lama & Notch Panjang Mengikuti Lirik Multi-Kalimat
- **Gejala:**
  1. Ketika lagu di-pause dan notch ditinggal beraktivitas lain, saat lagu di-play kembali (resume), lirik lagu tidak muncul sama sekali di Compact Notch.
  2. Kalimat lirik yang panjang atau terdiri dari beberapa kalimat terpotong `...` (ellipsis) karena lebar notch dibatasi kaku.
- **Akar Masalah:**
  1. Di `Windows/MainWindow.Progress.cs`, `shouldRunLyrics` sebelumnya disetel `_isExpanded && !_isMusicExpanded`. Akibatnya, saat notch berada dalam mode Compact, `_lyricsTimer` **dimatikan secara total**. Selain itu, saat lagu di-resume (`result.IsNewTrack == false`), pemanggilan lirik tidak dipicu ulang sehingga lirik tetap kosong.
  2. Batas pelebaran notch pada `UpdateCompactLyricsDisplay` dan `SetCompactModeContent` sebelumnya dibatasi kaku pada `440px`. Kalimat lirik panjang dengan lebar 350–500px tidak muat di kolom tengah dan terpotong secara prematur.
- **Solusi Kode:**
  1. **Nyalakan `_lyricsTimer` di Mode Compact Saat Media Playing:**
     ```csharp
     bool shouldRunLyrics = (_isExpanded && !_isMusicExpanded) ||
                            (_isMusicCompactMode && _settings.ShowCompactLyrics && (_currentMediaInfo?.IsPlaying ?? false));
     SetTimerEnabled(_lyricsTimer, shouldRunLyrics);
     ```
  2. **Auto-Trigger Lirik Saat Resume Playback:**
     Di `MainWindow.Media.cs`, saat `info.IsPlaying` kembali bernilai true untuk track yang sama:
     - Jika cache `_currentLyrics` sudah ada: langsung panggil `UpdateLyricsDisplay()` detik itu juga.
     - Jika cache kosong: panggil `FetchLyricsForTrack(info)` untuk mengunduh ulang lirik LRCLIB.
  3. **Lebar Dinamis Penuh Mengikuti Panjang Lirik:**
     Di `MainWindow.Lyrics.cs` dan `MainWindow.CompactPill.cs`, batas pelebaran dibuka leluasa hingga `700px` (`SystemParameters.PrimaryScreenWidth * 0.70`). Notch otomatis memanjang secara mulus (`CubicEase`, `300ms`) mengikuti seluruh kalimat lirik tanpa terpotong, dan menyusut kembali saat kalimat pendek.
  - File terkait: `Windows/MainWindow.Progress.cs`, `Windows/MainWindow.Media.cs`, `Windows/MainWindow.Lyrics.cs`, `Windows/MainWindow.CompactPill.cs`.

---

## 17. Perbaikan Lirik Saling Timpa Saat Ganti Lagu Mendadak di Tengah Pemutaran
- **Gejala:** Saat lagu diganti mendadak di tengah-tengah pemutaran (misal melompat dari lagu A ke lagu B), teks lirik lagu lama dan judul lagu baru saling bertumpuk (overlap/collision) di area tengah Compact Notch.
- **Akar Masalah:**
  1. Kontrol `CompactLyricText` dan `CompactTrackFallbackText` terletak di cell Grid yang sama dan sebelumnya hanya menganimasikan properti `Opacity` tanpa menyetel `Visibility = Visibility.Collapsed` dan tanpa mengosongkan `.Text = ""`.
  2. Saat lagu berganti mendadak, sisa animasi lirik lagu lama belum selesai memudar, sementara teks judul lagu baru mulai masuk. Kedua TextBlock sama-sama aktif dan langsung tercetak bertumpuk di titik koordinat yang sama.
  3. `ClearLyrics()` sebelumnya menunda pembersihan teks selama 300ms di dalam callback fade out, sehingga balapan dengan unduhan lirik baru.
- **Solusi Kode:**
  1. **Instant Wipeout di `ClearLyrics()`:** Detik itu juga hentikan seluruh clock animasi (`BeginAnimation(null)`), kosongkan teks ke string kosong (`.Text = ""`), dan setel `Visibility = Visibility.Collapsed` pada `CompactLyricText`, `CompactTrackFallbackText`, `LyricTextA`, dan `LyricTextB`.
  2. **Strict Exclusivity:** Pada `UpdateCompactLyricsDisplay` dan `SetCompactModeContent`:
     - Jika lirik aktif: paksa fallback judul `Visibility = Visibility.Collapsed` dan `Text = ""`.
     - Jika lirik belum ada / instrumental: paksa lirik `Visibility = Visibility.Collapsed` dan `Text = ""`.
  - File terkait: `Windows/MainWindow.Lyrics.cs`, `Windows/MainWindow.CompactPill.cs`.

---

## 18. Zero-Latency Pause & Fast-Path LRCLIB Smart Search (<400ms)
- **Gejala:**
  1. Saat video/musik di YouTube di-pause, status di notch lambat berhenti dan teks lirik terus berjalan sendiri padahal video sudah berhenti.
  2. Pencarian lirik untuk lagu YouTube membutuhkan waktu 10–13 detik karena rentetan 404/503.
- **Akar Masalah:**
  1. `ProgressEngine.cs` memiliki `PauseConfirmationWindow = 700ms` yang menolak mengonfirmasi status pause selama 700ms.
  2. `MainWindow.Lyrics.cs` memiliki rumus ekstrapolasi waktu `position = Position + (DateTime.UtcNow - LastUpdated)`. Selama status pause belum diakui, waktu jam komputer terus bertambah sehingga posisi lirik dipaksa maju 1 detik setiap detik.
  3. `LyricsService.cs` mengeksekusi antrean panjang kueri eksak durasi `/api/get` yang selalu gagal (404) pada lagu YouTube karena durasi video berbeda dari CD audio, sehingga memicu rate-limit 503 dari server LRCLIB.
- **Solusi Kode:**
  1. **Instant Pause Detection:** Di `MediaDetectionService.OnPlaybackChanged`, jika status sesi adalah `Paused`, langsung publikasikan `isPaused` ke UI dalam waktu < 5ms.
  2. **Zero-Latency Pause di `ProgressEngine`:** Hapus penundaan 700ms pada `ProcessPendingPauseConfirmation` sehingga `_state = ProgressState.Paused` aktif seketika.
  3. **Bekukan Posisi Lirik Total:** Di `MainWindow.Lyrics.cs`, jika status bukan Playing, hentikan penambahan waktu jam komputer dan bekukan `position`. Di `MainWindow.Progress.cs`, `LyricsTimer_Tick` hanya berjalan jika lagu benar-benar `Playing`.
  4. **Fast-Path Smart Search (<400ms):** Di `LyricsService.FetchUncachedLyricsAsync`, langsung panggil `/api/search?q={track} {artist}` di giliran pertama. Kueri ini tidak bergantung pada durasi detik eksak dan mengembalikan lirik lengkap dalam 1 panggilan HTTP saja tanpa terkena rate-limiting 503.
  - File terkait: `Services/LyricsService.cs`, `Services/ProgressEngine.cs`, `Services/MediaDetectionService.cs`, `Windows/MainWindow.Lyrics.cs`, `Windows/MainWindow.Progress.cs`.

---

## 19. Audit True Center Alignment, SVG Vector Paths & Eliminasi Sub-Pill
- **Gejala:**
  1. Ikon OSD dan cover album di sisi kiri terlihat turun/melorot lebih dekat ke tepi bawah kapsul.
  2. Logo mikrofon mute/unmute dan volume pecah atau tampak aneh karena dirender dari karakter font glyph.
  3. Saat mengarahkan mouse (hover) ketika lirik berjalan, muncul sub-pill kecil rusak di bawah notch.
- **Akar Masalah:**
  1. Kontainer `MusicCompactContent` sebelumnya memiliki 2 baris (Row 1 ditempati `CompactHoverInfo` yang memakan ruang vertikal) dan elemen-elemen di dalamnya memiliki margin vertikal yang tidak simetris.
  2. Penggunaan text glyph font untuk ikon sistem bergantung pada rendering font mesin pengguna yang tidak konsisten.
  3. Handler `AnimateThumbnailHover` menaikkan tinggi notch ke bawah untuk memunculkan marquee teks judul, menghasilkan bayangan sub-pill yang mengganggu transisi hover.
- **Solusi Kode:**
  1. **True Center Alignment:** `MusicCompactContent` disetel `HorizontalAlignment="Center"`, `VerticalAlignment="Center"`, `Margin="14,0"`. Seluruh kotak logo kiri (`CompactThumbnailBorder` & `CompactActionIconBorder`) diseragamkan ke `24x24 px` (`CornerRadius="6"`) dengan `VerticalAlignment="Center"` dan `Margin="0,0,10,0"`.
  2. **SVG Vector Paths:** Mengganti seluruh TextBlock glyph dengan elemen WPF `<Path>` menggunakan data SVG resmi:
     - Mic Muted: Kotak merah `#EF4444`, ikon putih `#FFFFFF` SVG path.
     - Mic Active: Kotak hijau `#22C55E`, ikon putih `#FFFFFF` SVG path.
     - Volume: Path speaker dinamis (mute, low, high).
     - Copied: Path centang tajam `#22C55E`.
  3. **Sub-Pill Disposed:** Mengubah `AnimateThumbnailHover` agar hanya melakukan scaling halus (1.08x) pada thumbnail tanpa memperbesar tinggi notch ke bawah, dan menyembunyikan `CompactHoverInfo`.
  - File terkait: `Windows/MainWindow.xaml`, `Windows/MainWindow.AnimationHelpers.cs`, `Windows/MainWindow.Controls.cs`, `Windows/MainWindow.SystemAlerts.cs`.

---

## 20. Pemulihan Album Art Chrome ke iTunes API Fallback & 1s Retry
- **Gejala:** Saat mengganti lagu di YouTube Music pada Chrome, stream thumbnail SMTC sempat kosong sehingga notch terjebak menampilkan ikon proses `chrome.exe` dan tidak mau ter-refresh.
- **Akar Masalah:**
  Chrome sempat tidak mengisi stream thumbnail SMTC di awal pergantian lagu. Sistem V-Notch membaca ikon aplikasi sebagai fallback dan menghentikan pencarian selanjutnya.
- **Solusi Kode:**
  1. Memasang flag `_hasRealAlbumArt = false` setiap kali lagu berganti atau saat thumbnail yang terdeteksi hanyalah ikon browser/placeholder ($\le 64\text{px}$).
  2. Menjalankan background task `TriggerAlbumArtRecovery`:
     - Jika dalam 1.5 detik cover asli belum diterima dari SMTC, lakukan kueri background ke **iTunes Search API** (`https://itunes.apple.com/search?term=...`). Ambil URL `artworkUrl100`, ubah menjadi `512x512bb`, unduh sebagai BitmapImage, lalu perbarui thumbnail notch secara otomatis.
     - Melakukan polling retry ke SMTC setiap 1 detik untuk menangkap stream resolusi penuh browser saat sudah siap.
  - File terkait: `Services/MediaDetectionService.cs`.

---

## 21. Smart Exclusive Playback Handover & Tombol Switch Media Source
- **Gejala:** Saat sedang memutar lagu lalu mulai memutar video YouTube, suara keduanya bertabrakan (double). Saat tombol pause ditekan di notch, perintah malah menembak video YouTube alih-alih lagu musik yang sedang ingin dimatikan.
- **Akar Masalah:**
  1. Windows SMTC mengizinkan multi-sesi aktif bersamaan tanpa mem-pause sesi sebelumnya.
  2. Handler play/pause sebelumnya mengirimkan global media key `VK_MEDIA_PLAY_PAUSE` yang ditembakkan secara acak ke window yang sedang memiliki fokus input Windows.
- **Solusi Kode:**
  1. **Smart Auto-Pause (Handover):** Pada `OnPlaybackChanged` dan pemindaian sesi `ResolveActiveSessionAsync`, saat Sesi B berpindah status menjadi `Playing`, sistem otomatis mengirimkan `await otherSession.TryPauseAsync()` ke Sesi A agar lagu lama langsung berhenti saat video baru diputar.
  2. **Targeted Play/Pause:** Di `MediaTransportControlService.cs`, tombol Play/Pause menembak langsung method `session.TryPauseAsync()` / `session.TryPlayAsync()` pada objek sesi yang aktif, tanpa menembakkan tombol keyboard global ke window lain.
  3. **Tombol Switch Media Source:** Menambahkan tombol `⇄ Switch Media` di Expanded View (`MediaPlaybackView.xaml`) untuk memungkinkan pengguna berpindah kendali antar-sesi secara instan ketika ada lebih dari 1 sesi media yang berjalan.
  - File terkait: `Services/MediaDetectionService.cs`, `Services/IMediaDetectionService.cs`, `Services/MediaTransportControlService.cs`, `Controls/MediaPlaybackView.xaml`, `Controls/MediaPlaybackView.xaml.cs`.

---

## 22. Eliminasi Total Jitter Idle, Kompensasi Offset Ruang Hitam Simetris & Standby Clock 10s
- **Gejala:**
  1. Kapsul notch kedut-kedut (bergetar/melompat) sendiri saat diam padahal pengguna tidak sedang melakukan apa-apa.
  2. Logo di dalam notch mentok ke dasar bawah kapsul tanpa ruang hitam di bawahnya.
  3. Bar volume menciut menjadi garis 1px (`|`).
- **Akar Masalah:**
  1. `AnimateContentCenterPop` (animasi scale 0.92 $\to$ 1.0) dipanggil berulang kali setiap kali timer heartbeat (500ms) atau volume berfluktuasi sedikit (noise < 1%), memicu puluhan animasi scale dan width saat idle.
  2. Bagian atas NotchBorder menempel ke bezel layar monitor, sehingga secara optik jika konten disetel di tengah geometris, logo tampak melorot ke dasar bawah.
  3. `CompactVolumeTrack` berada di kolom Grid `Auto` tanpa properti `Width` eksplisit, sehingga `ActualWidth` bernilai 0 dan kalkulasi pengisi volume menciut menjadi 1px.
- **Solusi Kode:**
  1. **Hapus Total `AnimateContentCenterPop`:** Menghilangkan pemicu scale transform berulang pada update UI.
  2. **Filter Volume Noise:** Pada `MainWindow.Controls.cs`, hanya tampilkan Volume OSD jika persentase bulat volume (`int`) berubah minimal 1% (`newVolumeInt != _lastVolumeInt`) atau status mute berubah, dan abaikan event inisialisasi pertama saat startup.
  3. **Kompensasi Offset Simetris:** Memberikan `Margin="14,0,14,4"` pada `MusicCompactContent` dan `Margin="0,0,0,4"` pada `CollapsedContent` sehingga jarak ruang hitam di atas dan di bawah kotak logo dan teks benar-benar seimbang dan presisi di layar.
  4. **Fixed Width Volume Bar (100px):** Menyetel `Width="100"` pada `CompactVolumeTrack` sehingga bar volume selalu berukuran 100px proporsional tanpa pernah menciut.
  5. **Standby Clock 10 Detik:** Jika media di-pause selama 10 detik tanpa ada aktivitas pengguna, notch secara otomatis beralih ke Mode Standby Jam (`HH:mm`), dan langsung beralih kembali saat lagu di-play atau tombol ditekan.
  - File terkait: `Windows/MainWindow.xaml`, `Windows/MainWindow.Controls.cs`, `Windows/MainWindow.CompactPill.cs`, `Services/VolumeService.cs`, `Services/MediaDetectionService.cs`.

---

## 23. Penanganan Konflik Tab Browser (Instagram Story / Web Lain) & Posisi Vertikal 100% Presisi
- **Gejala:**
  1. Saat pengguna mem-pause YouTube Music lalu membuka Instagram (misal menonton IG Story), notch menjadi bingung dan terus-menerus memunculkan kembali lagu YouTube Music yang sedang di-pause setiap kali slide story berganti/berhenti.
  2. Posisi logo dan teks di Compact Notch secara visual masih agak condong ke bawah kapsul hitam (jarak hitam atas dan bawah belum benar-benar simetris).
- **Akar Masalah:**
  1. Instagram di browser mengeluarkan audio tetapi tidak memperbarui metadata SMTC. Akibatnya SMTC browser tetap menyimpan judul lagu YouTube Music lama. Setiap kali slide story berganti atau memicu audio, status sesi berkedip antara Playing/Paused, sehingga V-Notch salah mengira lagu YouTube Music sedang diputar lagi.
  2. Ketika sesi YouTube Music di-pause dan notch sudah masuk ke Standby Clock, tidak ada penanda bahwa sesi paused tersebut sudah ditolak (*dismissed*). Akibatnya saat pemindaian SMTC fallback dijalankan, sesi paused tersebut diangkat kembali ke notch.
  3. `ApplyDynamicIslandContentAlignment` di `MainWindow.xaml.cs` sebelumnya menimpa `MusicCompactContent.VerticalAlignment = VerticalAlignment.Top` dan `Margin = new Thickness(8, 4, 8, 4)`, mengabaikan kompensasi offset bawah di XAML.
- **Solusi Kode:**
  1. **Lacak Last Playing & Dismissed Paused Session:** Di `MediaDetectionService.cs`, saat sesi berpindah ke `Playing`, catat sebagai `_lastActivePlayingSessionId` dan reset `_isDismissedPausedSession = false`. Saat sesi di-pause dan notch masuk ke Standby Clock (atau ditutup), panggil `_mediaService.DismissPausedSession()`. Sesi yang berstatus Paused dan sudah di-dismiss **dilarang keras ditampilkan kembali ke notch** kecuali statusnya benar-benar berubah menjadi `Playing`!
  2. **Deteksi Window Instagram / Media Sosial:** Menggunakan `Win32Interop.GetForegroundWindow` dan `GetWindowText`. Jika browser di foreground sedang membuka tab bertema media sosial (`Instagram`, `TikTok`, `Reels`), abaikan metadata lagu browser yang stale agar notch tidak terbangun oleh audio IG Story!
  3. **Debounce Pergantian Sesi 1500ms:** Menahan perpindahan sesi cepat (< 1500ms) saat video slide story berganti-ganti cepat agar notch tidak berkedip.
  4. **Presisi Posisi Vertikal Simetris:** Di `MainWindow.xaml` dan `ApplyDynamicIslandContentAlignment`, kunci `MusicCompactContent.Margin = new Thickness(14, 0, 14, 8)` dan `CollapsedContent.Margin = new Thickness(0, 0, 0, 8)` dengan `VerticalAlignment.Center`. Jarak ruang hitam di ATAS kotak logo 24x24 dan di BAWAH kotak logo 24x24 kini terukur sama persis di layar monitor.
  - File terkait: `Services/MediaDetectionService.cs`, `Services/IMediaDetectionService.cs`, `Windows/MainWindow.CompactPill.cs`, `Windows/MainWindow.xaml.cs`, `Windows/MainWindow.xaml`.

---

## 24. Eliminasi Total Auto-Pause Background & Restorasi Active Idle Mode (Jam + Status Terlihat)
- **Gejala:**
  1. Pengguna sama sekali tidak bisa memutar media apa pun di laptop (YouTube Music, YouTube biasa, maupun Instagram). Setiap kali tombol Play ditekan, media langsung ter-pause/stop otomatis oleh V-Notch.
  2. Saat kondisi idle di desktop tanpa memutar media, notch benar-benar menghilang (lenyap) dari layar.
- **Akar Masalah:**
  1. Di `MediaDetectionService.cs` (`OnPlaybackChanged`), terdapat perulangan otomatis `allSessions` yang memanggil `_ = other.TryPauseAsync()` setiap kali ada event playback. Perintah ini mengintervensi dan mematikan pemutar media browser pengguna di background.
  2. `FullscreenAutoHideController.cs` mengklasifikasikan jendela yang di-maximize tanpa caption sebagai `WindowedFullscreen` dan menyetel `WindowedFullscreen => true`, sehingga saat berada di desktop atau membuka browser biasa, notch langsung menganggap dirinya berada di fullscreen dan menyembunyikan diri (`Visibility = Collapsed`, `Opacity = 0`).
  3. `CollapsedContent` sebelumnya tidak diisi tanggal/baterai lengkap dan ukurannya tidak diatur proporsional saat beralih ke Standby.
- **Solusi Kode:**
  1. **HAPUS TOTAL `TryPauseAsync()` Otomatis:** Menghapus seluruh blok auto-pause di background. `MediaDetectionService` dijadikan **100% READ-ONLY (Pasif)** dan tidak pernah lagi mengirimkan perintah kontrol audio secara otomatis. Kontrol audio hanya boleh dipicu saat pengguna secara fisik mengklik tombol di UI.
  2. **Nonaktifkan Hide pada Windowed Maximized:** Di `FullscreenAutoHideController.cs`, tetapkan `WindowedFullscreen => false`. Notch **HANYA** boleh bersembunyi saat benar-benar berada di `ExclusiveFullscreen` (game, YouTube Fullscreen F11). Di luar kondisi tersebut, notch **WAJIB TETAP TERLIHAT di layar**.
  3. **Active Idle Mode (Jam + Tanggal/Baterai):** Saat tidak ada media yang berputar, `CollapsedContent` tetap berstatus `Visibility.Visible` dan `Opacity = 1.0` di tengah atas layar (`Margin="14,0,14,8"`):
     - Jam Digital tegas Putih (`HH:mm`) real-time.
     - Pemisah titik (`•`) + Hari/Tanggal (`Mon, 26 Oct`) dan status baterai (`⚡ 100%`) warna `#B0B0B0`.
     - Ikon SVG merah `StandbyMicBadge` jika mic sedang mute.
     - Kapsul berukuran proporsional (`Width="200–220"`).
  - File terkait: `Services/MediaDetectionService.cs`, `Controllers/FullscreenAutoHideController.cs`, `Windows/MainWindow.CompactPill.cs`, `Windows/MainWindow.xaml`, `Windows/MainWindow.xaml.cs`.
