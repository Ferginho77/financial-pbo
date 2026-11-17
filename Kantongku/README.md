# Kantongku — Manajemen Keuangan Pribadi & Kolaboratif (ASP.NET Core MVC .NET 8)

Aplikasi manajemen keuangan pribadi dan kolaboratif berbasis fitur **Kantong (Pockets)** ala Bank Jago, dibangun dengan ASP.NET Core MVC (.NET 8), Entity Framework Core, ASP.NET Core Identity, dan MySQL (`Pomelo.EntityFrameworkCore.MySql`).

## Menjalankan
1. Start **MySQL** di XAMPP Control Panel.
2. Sesuaikan `ConnectionStrings:DefaultConnection` di `appsettings.json` (default: `root` tanpa password, database `kantongku_db`).
3. Jalankan:
   ```powershell
   cd Kantongku
   dotnet restore
   dotnet run --launch-profile http
   ```
4. Buka http://localhost:5080 lalu daftar akun. Database dan seluruh tabel dibuat otomatis saat startup (`EnsureCreated` + upgrade kolom idempoten).

## Fitur Utama
- **Manajemen Kantong & Target Saldo:** Buat banyak kantong dengan warna aksen berbeda dan target saldo opsional dengan progress bar.
- **Pemasukan & Transfer Antar-Kantong Atomik:** Catat pemasukan langsung dan pindahkan dana antar-kantong dalam satu transaksi database.
- **Kategori Pengeluaran Bertingkat:** Kelompok sifat kebutuhan (*Primer*, *Sekunder*, *Tersier/Lainnya*, atau kustom) beserta alokasi spesifik + total pemakaian bulan berjalan.
- **Pencatatan, Edit & Pembatalan Transaksi:** Validasi saldo real-time & atomik di backend, edit nominal/kategori dengan penyesuaian saldo otomatis, dan pembatalan transfer dua arah.
- **Shared Pocket (Kantong Bersama):** Undang anggota via email dengan hak akses **Owner**, **Spender**, dan **Viewer** (ditegakkan di UI & backend), lengkap dengan audit trail pelaku transaksi.
- **Analitik & Mutasi Lengkap:** Filter bulan pada Dashboard + grafik interaktif Chart.js (Doughnut & Bar), pencarian/filter mutasi, serta ekspor mutasi CSV.
- **Profil & Keamanan Akun:** Ubah nama tampilan dan kata sandi melalui halaman pengaturan profil.

## Hak Akses Kantong
| Aksi | Owner | Spender | Viewer |
|---|---|---|---|
| Lihat saldo, mutasi & ekspor CSV | ✔ | ✔ | ✔ |
| Catat pengeluaran | ✔ | ✔ | ✘ |
| Edit / hapus pengeluaran sendiri | ✔ | ✔ | ✘ |
| Edit / hapus transaksi anggota lain | ✔ | ✘ | ✘ |
| Catat pemasukan, transfer, batalkan transfer | ✔ | ✘ | ✘ |
| Edit/hapus kantong & kelola anggota | ✔ | ✘ | ✘ |
