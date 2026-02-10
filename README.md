# Cinema System - System Rezerwacji biletó

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![License](https://img.shields.io/badge/license-MIT-blue)]()
[![Version](https://img.shields.io/badge/version-1.0.0-blue)]()
[![Document](https://img.shields.io/badge/documentation-complete-success)]()
### Zarządzanie Rezerwacjami
-  Rezerwacja biletów dla klientów
-  Wybór miejsc: zaznaczenie prostokątne na mapie sali lub drag-to-select w oknie podsumowania transakcji
-  Kody rabatowe (discount codes)
-  Automatyczne vouchery (2 bilety = 1 zestaw barowy)

### Zarządzanie Seansami
-  Zarządzanie salami i miejscami
-  Plan tygodniowy
-  Katalog filmów i reżyserów

### Raporty & Analityka
- Zaawansowane raporty PDF (QuestPDF)
- Wykresy sprzedaży
- KPI cards (revenue, avg price, occupancy)
- Eksport do CSV
- Statystyki per sala, per film

### Dodatkowe Funkcje
-  Multi-monitor support (ekran dla klienta)
-  Generowanie kodów QR na biletach
-  Wysyłanie biletów mailem (SMTP/Gmail)
-  3D Carousel (efekt CoverFlow)
-  Interfejs dark-theme

---

##  Wymagania

| Komponent | Wersja |
|-----------|--------|
| System Operacyjny | Windows 10+ |
| .NET Runtime | 10.0 |
| RAM | 512 MB |
| Dysk | 100 MB |
| Monitor | 1024x720 |

---

##  Szybki Start

### 1. Klonowanie
```bash
git clone https://github.com/1Stazy/CA.git
cd CA
```

### 2. Budowanie
```bash
dotnet restore
dotnet build CA.sln -c Release
```
```
### 2.5 Połączeni z Bazą:

..plik konfiguracji bazy i połączenia..

```
### 3. Uruchamianie
```bash
dotnet run --project CinemaSystem.Desktop.csproj
```

### 4. Pierwsze Logowanie
```
Username: admin
Password: admin
```

---

##  Struktura Katalogów

```
CA/
├── Models/              # 10 Modeli danych (User, Film, Screening, etc.)
├── ViewModels/          # 12 ViewModeli (MainVM, LoginVM, ReportsVM, etc.)
├── Views/               # 14 Widoków (LoginView, MoviesView, SeatSelectionView, etc.)
├── Services/            # 14 Serwisów (Auth, PDF, Email, Reports, etc.)
├── Migrations/          # Entity Framework migrations
├── Resources/           # Obrazy, ikony, style XAML
├── Core/                # ViewModelBase
│
├── App.xaml             # Zasoby globalne (style, konwertery)
├── App.xaml.cs          # Punkt startowy aplikacji
├── MainWindow.xaml      # Główne okno
├── MainWindow.xaml.cs   # Logic-behind
│
├── CinemaSystem.Desktop.csproj  # Plik projektu
└── README.md                    # Ten plik
```



## Stack Technologiczny

### Backend
- **C# 10+** - Język
- **.NET 10.0** - Framework
- **Entity Framework Core** - ORM
- **MVVM ToolKit** - Pattern implementation

### Frontend
- **WPF** - Interfejs
- **XAML** - Markup

### Biblioteki
- **QuestPDF** - Raporty PDF
- **QRCoder** - Kody QR
- **System.Net.Mail** - Email (SMTP)
- **System.Windows.Forms** - Multi-monitor

### Baza
- **SQLite** - Baza danych

---

## Dokumentacja

-  **Komentarze w kodzie** - Wszystkie pliki mają komentarze dotyczącą ich działania
  - 10 modeli
  - 12 ViewModeli
  - 14 Services
  - 14 Views



---

## Bezpieczeństwo

### Wdrożone
- Password hashing
- Input validation
- Null coalescing
- EF Core (SQL injection protection)

---

## Licencja

Projekt jest dostępny na licencji **MIT**.

---

**Ostatnia aktualizacja:** 7 lutego 2026  
**nr indeksu: 135 398 hasło okoń**
