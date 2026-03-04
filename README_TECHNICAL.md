# README – System Rezerwacji Biletów Kinowych

## 🎬 Przegląd Projektu

**Cinema System Desktop** to aplikacja WPF (.NET 10) do zarządzania rezerwacjami biletów kinowych. System wspiera:

- **Kasjerów:** Panel administratora do zarządzania filmami, seansami, rezerwacjami
- **Klientów:** Interface do przeglądania filmów, rezerwacji miejsc i zakupu biletów

### Kluczowe Funkcjonalności

✅ Autentykacja użytkowników (rola-based: Admin/Cashier)  
✅ Katalog filmów z wyszukiwaniem i filtrowaniem  
✅ Rezerwacja miejsc – interaktywna mapa sali  
✅ Typy biletów (normalny, ulgowy, student) z dynamiczną ceną  
✅ Kody promocyjne i rabaty  
✅ Generowanie PDF biletów z kodami QR  
✅ Raporty sprzedaży i statystyki  
✅ Dual-display support (okno kasjera + klienta)  
✅ Obsługa PostgreSQL (production) / SQLite (development)  

---

## 🏗️ Architektura

```
┌─────────────────────────┐
│   Prezentacja (WPF)     │  MainWindow, CustomerWindow
│   XAML + Data Binding   │
└────────────┬────────────┘
             │
┌────────────▼────────────┐
│  ViewModels (MVVM)      │  MainVM, LoginVM, DashboardVM, MoviesVM,
│  Logika Prezentacji     │  MovieDetailVM, SeatSelectionVM,
│  State Management       │  TicketSummaryVM
└────────────┬────────────┘
             │
┌────────────▼────────────┐
│  Services & Models      │  AuthenticationService, CinemaDbContext,
│  Encje Biznesowe        │  Film, Screening, Ticket, Reservation
└────────────┬────────────┘
             │
┌────────────▼────────────┐
│  Entity Framework Core  │  PostgreSQL / SQLite ORM
│  Warstwa Danych         │
└────────────┬────────────┘
             │
┌────────────▼────────────┐
│  Baza Danych            │  PostgreSQL (prod)
│  PostgreSQL / SQLite    │  SQLite (dev)
└─────────────────────────┘
```

---

## 📋 Wymagania Systemowe

- **OS:** Windows 10 / 11 / Server 2019+
- **.NET:** .NET 10 SDK (lub Runtime)
- **Database:**
  - **Production:** PostgreSQL 12+ (⭐ główny system bazy danych)
  - **Development:** SQLite (opcjonalnie, do szybszego prototypowania)

### PostgreSQL – Główny System Bazy Danych

System **opiera się na PostgreSQL** jako produkcyjnym systemie zarządzania bazą danych. PostgreSQL zapewnia:

✅ Enterprise-grade reliability i stabilność  
✅ Wysoką wydajność i skalowanie dla dużych zbiorów rezerwacji  
✅ Pełne wsparcie ACID transakcji (krytyczne dla operacji finansowych)  
✅ Wbudowane wsparcie dla connection pooling (Npgsql)  
✅ Łatwy deployment na chmurze (Heroku, Railway, Render, AWS RDS, Azure)  
✅ Optymistyczne blokowanie z wykorzystaniem RowVersion (timestamp)  

**Instalacja PostgreSQL:**

```bash
# Windows (Chocolatey)
choco install postgresql

# Linux (Ubuntu/Debian)
sudo apt-get install postgresql postgresql-contrib

# macOS (Homebrew)
brew install postgresql

# Lub: https://www.postgresql.org/download/
```

### Zależności (NuGet)

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.1" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
<PackageReference Include="QRCoder" Version="1.7.0" />
<PackageReference Include="QuestPDF" Version="2025.12.1" />
```

---

## 🚀 Szybki Start

### 1. Klonowanie Repozytorium

```bash
git clone https://github.com/yourusername/CinemaSystem.git
cd CinemaSystem/CA_TEST
```

### 2. Konfiguracja Bazy Danych PostgreSQL

Aplikacja **opiera się na PostgreSQL** dla środowiska produkcyjnego.

#### Development (PostgreSQL localhost)

```bash
# 1. Zainstaluj PostgreSQL (patrz Wymagania Systemowe powyżej)

# 2. Otwórz psql i utwórz bazę
psql -U postgres
CREATE DATABASE cinema_db;

# 3. Domyślny connection string w kodzie:
# Host=localhost;Database=cinema_db;Username=postgres;Password=postgres;Port=5432

# 4. Uruchom aplikację – migracje EF Core będą zastosowane automatycznie
```

#### Production (PostgreSQL Cloud Hosting)

```bash
# Ustawić zmienną środowiskową DATABASE_URL:

# Windows CMD:
set DATABASE_URL=postgresql://user:password@host.com:5432/cinema_db

# PowerShell:
$env:DATABASE_URL="postgresql://user:password@host.com:5432/cinema_db"

# Linux/Mac:
export DATABASE_URL="postgresql://user:password@host.com:5432/cinema_db"

# Heroku example:
heroku config:set DATABASE_URL="postgresql://..."
```

**Popularne PostgreSQL Hosting Services:**
- **Railway**: railway.app (1-click PostgreSQL)
- **Render**: render.com (free tier PostgreSQL)
- **Heroku**: heroku.com (PostgreSQL add-on)
- **AWS RDS**: aws.amazon.com/rds/postgresql
- **Azure**: azure.microsoft.com/services/postgresql

#### Development Fallback (SQLite – opcjonalnie)

```bash
# Jeśli PostgreSQL nie jest dostępny, baza zostanie automatycznie stworzona jako SQLite
# Plik: bin/Debug/kino.db

# UWAGA: SQLite przeznaczone tylko dla testowania – w produkcji zawsze używaj PostgreSQL
```

### 3. Restauracja Pakietów

```bash
dotnet restore CinemaSystem.Desktop.csproj
```

### 4. Budowanie

```bash
dotnet build CinemaSystem.Desktop.csproj
```

### 5. Uruchamianie

```bash
dotnet run
```

---

## 📊 Struktura Projektu

```
CinemaSystem.Desktop/
│
├── 📄 App.xaml                          # Application config
├── 📄 App.xaml.cs                       # Entry point
├── 📄 MainWindow.xaml                   # Staff UI
├── 📄 MainWindow.xaml.cs                # Staff code-behind
│
├── 📁 Core/
│   └── 📄 ViewModelBase.cs              # Base ViewModel class
│
├── 📁 Models/                           # Domain Entities
│   ├── 📄 Film.cs
│   ├── 📄 Screening.cs
│   ├── 📄 Hall.cs
│   ├── 📄 Seat.cs
│   ├── 📄 Reservation.cs
│   ├── 📄 Ticket.cs
│   ├── 📄 User.cs
│   ├── 📄 Director.cs
│   ├── 📄 PromoCode.cs
│   ├── 📄 TicketTypeDefinition.cs
│   └── 📄 Advertisement.cs
│
├── 📁 ViewModels/                       # MVVM Presentation Logic
│   ├── 📄 MainViewModel.cs              # Orchestrator
│   ├── 📄 LoginViewModel.cs             # Authentication
│   ├── 📄 DashboardViewModel.cs         # Navigation hub
│   ├── 📄 MoviesViewModel.cs            # Catalog
│   ├── 📄 MovieDetailViewModel.cs       # Film details
│   ├── 📄 SeatSelectionViewModel.cs     # Seat map
│   ├── 📄 TicketSummaryViewModel.cs     # Checkout
│   ├── 📄 ScheduleViewModel.cs          # Screenings schedule
│   ├── 📄 ReportsViewModel.cs           # Sales reports
│   └── 📄 TicketManagementViewModel.cs  # Edit/return tickets
│
├── 📁 Views/                            # XAML UI
│   ├── 📄 LoginView.xaml
│   ├── 📄 DashboardView.xaml
│   ├── 📄 MoviesView.xaml
│   ├── 📄 MovieDetailView.xaml
│   ├── 📄 SeatSelectionView.xaml
│   ├── 📄 TicketSummaryView.xaml
│   ├── 📄 ScheduleView.xaml
│   ├── 📄 ReportsView.xaml
│   └── 📄 TicketManagementView.xaml
│
├── 📁 Services/                         # Business Services
│   ├── 📄 CinemaDbContext.cs            # EF Core DbContext
│   ├── 📄 AuthenticationService.cs      # Login verification
│   ├── 📄 ScreenManager.cs              # Multi-monitor support
│   ├── 📄 EmailService.cs               # Send emails
│   ├── 📄 ReportSettingsService.cs      # Report generation
│   └── 📄 DatabaseGenerator.cs          # DB initialization
│
├── 📁 Migrations/                       # EF Core Migrations
│   ├── 📄 20260106155819_InitialCreate.cs
│   └── 📄 20260106184120_AddHalls.cs
│
├── 📁 Resources/
│   └── 📁 Images/                       # Posters, icons
│
└── 📄 CinemaSystem.Desktop.csproj       # Project file

```

---

## 🔐 Domyślne Dane Logowania

**Development Only – ZMIEŃ NA PRODUKCJI!**

| Login | Password | Role |
|-------|----------|------|
| `admin` | `admin123` | Admin |
| `cashier` | `cashier123` | Cashier |

⚠️ **TODO:** Implementacja bcrypt password hashing!

---

## 🌊 Przepływ Rezerwacji

```
1. Zalogowanie (LoginView)
   ↓
2. Dashboard → Wybór Filmów (MoviesView)
   ↓
3. Klik Film → Szczegóły + Seanse (MovieDetailView)
   ↓
4. Klik "Kupić" → Wybór Seansu
   ↓
5. Mapa Sali (SeatSelectionView)
   ├─ Wyświetli wolne/zajęte miejsca
   ├─ Multi-select miejsca
   └─ Klik "Dalej"
   ↓
6. Podsumowanie (TicketSummaryView)
   ├─ Review koszyka
   ├─ Wybór typu biletu per miejsce
   ├─ [Optional] Kod promocyjny
   └─ Klik "Potwierdź Rezerwację"
   ↓
7. Database Transaction
   ├─ INSERT Reservation
   ├─ INSERT Tickets (1:N)
   └─ COMMIT
   ↓
8. PDF Generation + Email
   ├─ QuestPDF creates PDF
   ├─ QRCoder generates codes
   └─ EmailService sends to customer
   ↓
9. Potwierdzenie
   └─ "Rezerwacja udana! Bilet zapisany na pulpicie."
```

---

## 📝 Konfiguracja (Development vs Production)

### Development

- **Baza:** SQLite (`kino.db` w folderze aplikacji)
- **Logging:** Wyłączone
- **Auth:** Plain text password (TODO: hashing)
- **API:** Local only

**Zmiana w `Services/CinemaDbContext.cs`:**
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    // Development default – localhost PostgreSQL
    var connectionString = "Host=localhost;Database=cinema_db;Username=postgres;Password=postgres;Port=5432";
    optionsBuilder.UseNpgsql(connectionString, ...);
    
    #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging(true);  // Show query params in logs
    #endif
}
```

### Production

- **Baza:** PostgreSQL (external server)
- **Connection String:** Zmienna `DATABASE_URL`
- **Logging:** Serilog (TODO)
- **Auth:** JWT + bcrypt password hashing (TODO)

**Deployment:**
```bash
# Heroku, Railway, Render, etc.
heroku config:set DATABASE_URL="postgresql://user:password@host:5432/db"
dotnet publish -c Release
```

---

## 🧪 Testing

### Unit Tests (TODO)

```bash
dotnet test
```

### Manual Testing Checklist

- [ ] Login z poprawnym hasłem
- [ ] Login z błędnym hasłem → error message
- [ ] Wczytaj listę filmów
- [ ] Filtruj filmy po nazwie (search)
- [ ] Klik film → pokaż szczegóły + seanse
- [ ] Klik seans → mapa sali wyświetli się
- [ ] Select place → status zmienia się na Selected
- [ ] Check summary → cena się przelicza
- [ ] Add promo code → rabat się stosuje
- [ ] Confirm reservation → DB updated
- [ ] PDF generated + saved to Desktop
- [ ] Logout → powrót do LoginView

---

## 🔧 Troubleshooting

### Problem: "Baza nie istnieje"

**Rozwiązanie:**
```csharp
// W App.xaml.cs – DatabaseGenerator.GenerateFinalDatabase() ma to obsłużyć
// Jeśli nie, ręcznie:
using (var context = new CinemaDbContext())
{
    context.Database.EnsureCreated();
    context.Database.Migrate();
}
```

### Problem: "Connection to PostgreSQL failed"

**Rozwiązanie:**
- Sprawdź, czy PostgreSQL biegnie: `psql -U postgres`
- Sprawdź connection string w `CinemaDbContext`
- Jeśli production: sprawdź zmienną `DATABASE_URL`

### Problem: "No migrations pending"

Wszystkie migracje zastały już zastosowane. Jeśli chcesz dodać nową feature:
```bash
dotnet ef migrations add AddNewFeature
dotnet ef database update
```

---

## 📈 Roadmap Rozwoju

### Phase 1 (Current)
- ✅ Basic MVVM architecture
- ✅ Film catalog + screening management
- ✅ Seat selection with visual map
- ✅ Ticket generation + PDF export
- ✅ Promo codes support

### Phase 2 (Next)
- [ ] Full DI Container (Microsoft.Extensions.DependencyInjection)
- [ ] Centralized Logging (Serilog)
- [ ] Password hashing (BCrypt)
- [ ] Unit Tests + Integration Tests

### Phase 3 (Future)
- [ ] JWT Authentication
- [ ] Web API (ASP.NET Core REST)
- [ ] Mobile App (Xamarin/MAUI)
- [ ] Advanced Analytics
- [ ] Machine Learning (Movie recommendations)
- [ ] Payment Gateway Integration (Stripe/PayU)

---

## 📚 Dokumentacja

Pełna dokumentacja techniczna dostępna w: [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)

**Zawiera:**
- Architektura systemowa (diagramy Mermaid)
- Specyfikacja każdego komponentu
- Schemat bazy danych (ERD)
- Przepływy procesowe (workflow diagrams)
- Development guide
- Best practices & patterns

---

## 🤝 Wkład (Contributing)

1. Fork project
2. Utwórz feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

**Code Style:** 
- PascalCase dla public members
- camelCase dla private fields
- Komentuj "dlaczego", nie "co"

---

## 📄 Licencja

MIT License – patrz [LICENSE](LICENSE) dla detali.

---

## 👨‍💼 Autor & Kontakt

**Senior Technical Architect**  
.NET/WPF Expert | Clean Architecture Enthusiast

📧 Email: dev@example.com  
🐙 GitHub: [@yourprofile](https://github.com)

---

## 📞 Support

**Znalazłeś bug?**
1. Sprawdź [Issues](https://github.com/yourusername/CinemaSystem/issues)
2. Otwórz nowy issue z details (OS, .NET version, logs)

**Pytania?**
- Discord: [Join Server](#)
- Email: support@example.com

---

**Last Updated:** March 2026 | .NET 10 | Version 1.0.0
