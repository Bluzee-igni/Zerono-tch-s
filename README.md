<p align="center">
  <img src="Assets/logo.png" width="100" height="100" alt="Zerono-tch-s">
</p>

<h1 align="center">Zerono-tch-s</h1>
<h3 align="center">Enhanced V-Notch Personal Edition</h3>

<p align="center">
  A feature-packed, lightweight desktop status and media companion for Windows 10 & 11.<br>
  An enhanced personal build of the open-source <a href="https://github.com/rainaku/V-Notch"><b>rainaku/V-Notch</b></a> project.
</p>

<p align="center">
  <a href="https://github.com/Bluzee-igni/Zerono-tch-s">
    <img src="https://img.shields.io/badge/Repository-Bluzee--igni%2FZerono--tch--s-0A84FF?style=for-the-badge&logo=github" alt="Repository">
  </a>
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-ffffff?style=for-the-badge&labelColor=222222&logo=windows&logoColor=ffffff" alt="Windows 10 / 11">
  <img src="https://img.shields.io/badge/.NET-8.0-ffffff?style=for-the-badge&labelColor=222222&logo=dotnet&logoColor=512BD4" alt=".NET 8.0">
  <a href="LICENSE">
    <img src="https://img.shields.io/badge/License-Apache%202.0-30D158?style=for-the-badge&labelColor=222222" alt="License">
  </a>
  <a href="https://github.com/Bluzee-igni/Zerono-tch-s/stargazers">
    <img src="https://img.shields.io/github/stars/Bluzee-igni/Zerono-tch-s?style=for-the-badge&color=FFD60A&labelColor=222222&logo=apachespark&logoColor=FFD60A" alt="Stars">
  </a>
</p>

<p align="center">
  <a href="https://github.com/Bluzee-igni/Zerono-tch-s/releases">Download</a> ·
  <a href="#fitur-unggulan--enhancements-di-zerono-tch-s">Fitur Baru</a> ·
  <a href="#sumber-asli--original-upstream-attribution">Sumber Asli (Upstream)</a> ·
  <a href="#pernyataan-non-komersial--open-source">Lisensi & Non-Komersial</a> ·
  <a href="#panduan-penggunaan-usage">Cara Pakai</a>
</p>

---

> [!IMPORTANT]
> ### 📢 Pernyataan Non-Komersial & Open-Source / Non-Commercial & Open-Source Statement
>
> 🇮🇩 **Bahasa Indonesia:**
> 1. **100% Non-Komersial**: Repositori dan proyek ini **TIDAK AKAN PERNAH dikomersialkan atau diperjualbelikan** dalam bentuk apa pun.
> 2. **Kebutuhan Pribadi & Pembelajaran**: Modifikasi serta penambahan fitur arsitektur pada repositori ini dikerjakan murni untuk memenuhi kebutuhan pribadi pengembang (**Bluzee-igni** sebagai pelajar/siswa IT) dalam menunjang produktivitas harian.
> 3. **Sepenuhnya Gratis & Terbuka**: Seluruh source code dan aplikasi ini **100% gratis untuk diunduh (free to download)**, digunakan, dan dipelajari oleh siapa saja yang membutuhkan, dengan tetap menghormati dan mematuhi lisensi open-source aslinya: **[Apache License 2.0](LICENSE)** dari proyek upstream **[rainaku/V-Notch](https://github.com/rainaku/V-Notch)**.
>
> ---
>
> 🇬🇧 **English:**
> 1. **100% Non-Commercial**: This repository and project is strictly **NON-COMMERCIAL** and will **NEVER be sold or monetized** in any form.
> 2. **Personal & Educational Purpose**: Architectural enhancements and feature implementations in this repository are developed purely for personal daily productivity needs as an IT student.
> 3. **Completely Free to Download & Study**: The application and entire source code are **100% free to download**, use, and inspect for anyone interested, while fully honoring and adhering to the original open-source license: **[Apache License 2.0](LICENSE)** from upstream **[rainaku/V-Notch](https://github.com/rainaku/V-Notch)**.

---

## 🌟 Sumber Asli / Original Upstream Attribution

Proyek ini dibangun di atas fondasi aplikasi open-source luar biasa karya **[rainaku](https://github.com/rainaku)**:
- **Repository Asli Upstream:** [https://github.com/rainaku/V-Notch](https://github.com/rainaku/V-Notch)
- **Website Resmi V-Notch:** [https://v-notch.vercel.app](https://v-notch.vercel.app)
- **Author Asli:** [rainaku](https://rainaku.id.vn) · [Facebook](https://www.facebook.com/rain.107/)
- **Dukungan untuk Author Asli:** Jika Anda menyukai V-Notch original, Anda dapat mendukung pembuat aslinya melalui [PayPal rainaku](https://www.paypal.me/PhuocLe678).
- **Proyek Lain oleh rainaku:** [Vertex](https://github.com/rainaku/Vertex) (Radial file converter) dan [Scroll-V](https://github.com/rainaku/Scroll-V) (Smooth scrolling utility).

---

## 🚀 Fitur Unggulan & Enhancements di Zerono-tch-s

Versi **Zerono-tch-s** menambahkan berbagai peningkatan arsitektur sistem, penyempurnaan ergonomi visual, dan integrasi hardware Windows yang tidak ada di versi standar:

### 1. 🎵 Adaptive Synced Lyrics di Compact Notch (Spotify & YouTube Music)
- **Lirik Real-Time di Notch Mini:** Menghidupkan area tengah notch kecil dengan lirik tersinkronisasi milidetik menggunakan database LRCLIB.
- **Dukungan Penuh Multi-Platform:** Tidak hanya Spotify desktop, kini mendukung penuh **YouTube Music PWA**, Google Chrome, Microsoft Edge, Firefox, dan Apple Music.
- **Smart Regex Sanitizer:** Pembersih otomatis embel-embel judul YouTube (`- Topic`, `VEVO`, `(Official Video)`, `[MV]`, `(Lyric Video)`) sebelum kueri lirik dikirim agar selalu mendapatkan hasil yang akurat.
- **Adaptive Width & Fade Transition:** Lebar notch beradaptasi mulus mengikuti panjang kalimat lirik dengan transisi fade & slide.
- **Instrumental & Title Fallback:** Saat intro lagu, instrumental, atau lagu tanpa lirik, notch menyusut lembut kembali ke ukuran standar dan menampilkan teks ringkas `Judul Lagu • Artis`.

### 2. 🔊 Hardware Volume OSD Real-Time (Anti-Jitter Debounce)
- **CoreAudio WASAPI Integration:** Menangkap penekanan tombol fisik keyboard (*Volume Up, Volume Down, Mute*) via Windows Audio Endpoint secara asinkron tanpa membebani CPU.
- **Visual OSD Responsif:** Menampilkan ikon speaker dinamis (🔊/🔉/🔇), bilah progress slider yang presisi, dan teks persentase angka (misal `65%` atau `Mute`).
- **Debounced Animation Engine:** Tidak me-restart animasi kapsul berulang-ulang saat volume ditekan berkali-kali secara cepat, sehingga tampilan tetap stabil tanpa getaran (*zero jitter*), lalu auto-hide lembut setelah 1.5 detik.

### 3. 🎙️ Hardware Microphone Monitor & Interactive Quick-Toggle
- **Penyadapan Status Mute Mic:** Memonitor endpoint mikrofon laptop bawaan (`DataFlow.Capture`) secara instan.
- **Alert Pill Sementara (1.5 Detik):** Memberikan notifikasi transisi elegan:
  - 🎙️ `Microphone Muted` (dengan teks aksen merah terang kontras).
  - 🟢 `Microphone Active` (dengan teks aksen hijau segar).
- **Persistent Status Badge:** Ikon 🔇 merah tetap menyala rapi di slot khusus sisi kanan notch selama mic masih mute agar kamu selalu ingat status mic saat voice call Discord, Zoom, atau Google Meet.
- **One-Click Quick Toggle:** Ikon merah ini interaktif—cukup diklik sekali untuk langsung mute/unmute mikrofon laptop secara instan!

### 4. 📋 Rich 3-Column Clipboard "Copied" OSD
- **Tampilan Berisi & Elegan:** Saat menekan `Ctrl+C`, notch menampilkan layout proporsional:
  - **Kiri:** Badge aksen hijau lembut dengan ikon checkmark (`✓`).
  - **Tengah:** Teks bold `Copied` disertai cuplikan teks yang disalin (maksimal 25–28 karakter dengan tanda kutip), atau label `Image` / `Files`.
  - **Kanan:** Ikon status checkmark.
- **Lebar Dinamis Proporsional (200px – 340px):** Notch menyempit/melebar secara pas mengikuti panjang cuplikan clipboard, tidak kosong melompong.

### 5. 💬 Discord & WhatsApp Web Notification Listener
- **WinRT + UI Automation Fallback:** Menyadap notifikasi toast Windows untuk pesan masuk dari Discord dan WhatsApp (baik aplikasi desktop maupun WhatsApp Web di browser Chrome/Edge/Brave/Firefox).
- **Notch Toast Banner:** Notch melebar menampilkan badge aplikasi (💬 / 🎮), nama pengirim pesan tebal, dan cuplikan teks pesan.
- **Click to Foreground:** Mengklik banner notifikasi pada notch akan langsung membawa jendela Discord atau WhatsApp ke latar depan (*foreground focus*).

### 6. 🖥️ Auto-Hide Saat Fullscreen (YouTube / Game / Video)
- **Deteksi Jendela Fullscreen:** Otomatis mendeteksi saat aplikasi di foreground berada dalam mode Fullscreen (misal saat menonton video YouTube F11 di browser, VLC, atau game Fullscreen).
- **Smooth Auto-Slide:** Notch otomatis meluncur keluar layar (`Opacity = 0`, `IsHitTestVisible = false`) agar tidak menutupi video atau kontrol game. Begitu keluar dari mode Fullscreen, notch meluncur kembali secara mulus.

### 7. 🖱️ Menu Klik Kanan Fungsional (ContextMenu)
- **Menu Cepat di Notch:** Klik kanan di area mana saja pada notch memunculkan menu berdesain modern:
  - `⚙ Open Settings`: Membuka jendela pengaturan V-Notch di tengah layar.
  - `🔄 Refresh Notch State`: Memulihkan tampilan notch dan mengevaluasi ulang sesi lirik/media.
  - `👁 Hide Notch (Until Next Hover/Click)`: Menyembunyikan notch sementara hingga kursor diarahkan ke sisi atas layar.
  - `🚪 Exit Application`: Menutup aplikasi secara bersih (`Application.Current.Shutdown()`).

### 8. ⚡ Zero-Click Auto-Resume & Debloated Engine
- **Strict Mutual Exclusion:** Setiap OSD aksi (Volume, Mute, Copied, Toast) mengunci tampilan sementara (`_isTransientOsdActive = true`) agar lirik tidak bertumpuk di atas OSD. Begitu selesai, notch langsung otomatis kembali ke lirik lagu atau jam digital tanpa memerlukan klik manual (*zero-click recovery*).
- **Debloated Lightweight Engine:** Membersihkan library AI lokal yang berat (`LLamaSharp` dan ONNX runtime) sehingga startup aplikasi sangat cepat dan hemat konsumsi RAM.

---

## 🎨 Tampilan Fitur Bawaan V-Notch Asli (Upstream Core)

Semua kecanggihan visual inti dari repositori upstream `rainaku/V-Notch` tetap dipertahankan secara utuh:

<div align="center">

<table align="center">
  <tr>
    <td align="center" valign="top" width="50%">
      <img src="Introduction/dynamic.gif" alt="Dynamic Island mode"><br>
      <b>Dynamic Island Mode</b><br>
      <sub>Kapsul mengambang dengan animasi pegas (spring) dan refraksi Liquid Glass.</sub>
    </td>
    <td align="center" valign="top" width="50%">
      <img src="Introduction/media-control.gif" alt="Media controls"><br>
      <b>Kontrol Pemutar Media</b><br>
      <sub>Kontrol playback, volume slider, tampilan album art, dan seek bar lagu.</sub>
    </td>
  </tr>
  <tr>
    <td align="center" valign="top" width="50%">
      <img src="Introduction/Spotify.gif" alt="Spotify and synced lyrics"><br>
      <b>Spotify &amp; Synced Lyrics</b><br>
      <sub>Lirik lagu tersinkronisasi di atas gradien warna album art yang dinamis.</sub>
    </td>
    <td align="center" valign="top" width="50%">
      <img src="Introduction/volume.gif" alt="Volume and audio mixer"><br>
      <b>Volume &amp; Audio Mixer</b><br>
      <sub>Atur volume masing-masing aplikasi atau master slider audio sistem.</sub>
    </td>
  </tr>
  <tr>
    <td align="center" valign="top" width="50%">
      <img src="Introduction/file-shelf.gif" alt="File shelf"><br>
      <b>File Shelf (Rak Berkas)</b><br>
      <sub>Letakkan file sementara ke atas notch, lalu seret ke aplikasi lain kapan saja.</sub>
    </td>
    <td align="center" valign="top" width="50%">
      <img src="Introduction/gesture.gif" alt="Gestures"><br>
      <b>Kontrol Gestur Mouse</b><br>
      <sub>Geser kiri/kanan untuk ganti lagu, scroll untuk beralih tampilan shelf.</sub>
    </td>
  </tr>
  <tr>
    <td align="center" valign="top" width="50%">
      <img src="Introduction/liquid-glass.gif" alt="Liquid Glass optics"><br>
      <b>DirectX 11 Liquid Glass</b><br>
      <sub>Efek kaca kristal dengan chromatic aberration dan refraksi tepi layar.</sub>
    </td>
    <td align="center" valign="top" width="50%">
      <img src="Introduction/spotlight.gif" alt="Spotlight search"><br>
      <b>Spotlight Launcher (Alt + Space)</b><br>
      <sub>Tekan Alt+Space untuk mencari aplikasi, file, kalkulator cepat, dan AI.</sub>
    </td>
  </tr>
</table>

</div>

---

## ⌨️ Panduan Penggunaan (Usage)

| Aksi Mouse / Keyboard | Hasil Tampilan |
| :--- | :--- |
| **Arahkan Kursor (Hover)** | Membuka notch menjadi ukuran penuh (Expanded View) |
| **Klik Kiri pada Notch** | Beralih antara mode compact (mini) dan mode expanded |
| **Klik Tengah (Middle Click)** | Play / Pause media lagu atau video |
| **Scroll Roda Mouse ke Bawah** | Berpindah ke rak file sementara (File Shelf) |
| **Scroll Roda Mouse ke Atas** | Kembali ke kontrol pemutar media |
| **Geser (Swipe) Kiri / Kanan** | Memutar lagu sebelumnya / lagu berikutnya |
| **Klik Kanan pada Notch** | Membuka ContextMenu (Settings, Refresh, Hide, Exit) |
| **Tekan Tombol Mic Laptop** | Muncul alert mute 1.5 detik & toggle badge merah 🔇 |
| **Tekan Tombol Volume Keyboard** | Menampilkan animasi volume slider OSD di tengah notch |
| **`Alt + Space`** | Membuka Spotlight Launcher untuk cari aplikasi atau kalkulator |

---

## 🛠️ Kompilasi & Build Mandiri (Building from Source)

Aplikasi ini menggunakan **.NET 8.0 SDK (Windows Desktop)**:

1. **Clone repositori ini:**
   ```bash
   git clone https://github.com/Bluzee-igni/Zerono-tch-s.git
   cd Zerono-tch-s
   ```
2. **Kompilasi proyek dengan .NET CLI:**
   ```bash
   dotnet build -c Debug
   ```
3. **Jalankan aplikasi:**
   ```bash
   dotnet run
   ```
   *Atau jalankan file executable siap pakai yang dihasilkan di:*
   `bin\Debug\net8.0-windows10.0.19041.0\win-x64\V-Notch.exe`

---

## 📚 Dokumentasi Teknis Tambahan
- **[desain.md](desain.md)**: Panduan arsitektur visual asli (Sistem Font, Palet Warna, Dimensi Geometri, Animasi, dan Peta `x:Name` XAML).
- **[BUGFIX_DOCS.md](BUGFIX_DOCS.md)**: Dokumentasi teknis mendalam seluruh riwayat bug dan solusi arsitektur tambal sulam.
- **[THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md)**: Pemberitahuan lisensi pihak ketiga dan pustaka open-source upstream.

---

## 📄 Lisensi (License)

Proyek ini dilisensikan di bawah **Apache License 2.0**. Lihat berkas **[LICENSE](LICENSE)** untuk ketentuan lisensi lengkap.

```text
Copyright (c) 2026 rainaku (Original Author)
Copyright (c) 2026 Bluzee-igni (Enhanced Personal Edition Modifications)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0
```

<p align="center">
  Forked with respect from <a href="https://github.com/rainaku/V-Notch"><b>rainaku/V-Notch</b></a> ·
  Maintained for personal educational productivity by <a href="https://github.com/Bluzee-igni"><b>Bluzee-igni</b></a>
</p>
