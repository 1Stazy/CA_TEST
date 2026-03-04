# Dokumentacja Techniczna – System Rezerwacji Biletów Kinowych

**Rola:** Senior Technical Architect | Lead Technical Writer  
**Kontekst:** Aplikacja WPF (.NET 10) z architekturą MVVM  
**Data:** Marzec 2026  
**Status:** Kompletna dokumentacja ekspercka

---

## Spis Treści

1. [Architektura i High-Level Design](#architektura-i-high-level-design)
2. [Specyfikacja Komponentów](#specyfikacja-komponentów)
3. [Warstwa Danych i Persystencja](#warstwa-danych-i-persystencja)
4. [Przepływy Procesowe](#przepływy-procesowe)
5. [Zależności i Rozwój](#zależności-i-rozwój)

---

## Architektura i High-Level Design

### Wstęp: Cel Biznesowy i Zakres Systemu

**System Rezerwacji Biletów Kinowych** (Cinema System Desktop) to aplikacja desktopowa klasy enterprise przeznaczona do zarządzania rezerwacjami biletów do kin. System wspiera dwie kategorie użytkowników:

- **Kasjerzy (Staff):** Obsługują panel administratora do zarządzania filmami, seansami, rezerwacjami i raportami sprzedaży
- **Klienci (Customers):** Interaktywnie przeglądają filmy, wybierają seanse, rezerwują miejsca i finalizują transakcje

**Kluczowe funkcjonalności:**
- Uwierzytelnianie użytkowników (rola-based access control)
- Zarządzanie katalogiem filmów i seansów
- Rezerwacja miejsc w sali kinowej (mapa sali)
- System biletów ze wspomaganiem różnych typów (normalny, ulgowy, student)
- Kody promocyjne i rabaty
- Generowanie raportów sprzedaży i statystyk
- Drukowanie/export biletów do PDF
- Zarządzanie halami kinowymi (layout, pojemność)

**Zakresy techniczne:**
- Język: C# (.NET 10)
- Platforma: WPF (Windows Presentation Foundation)
- Architektura: MVVM + Entity Framework Core
- Baza Danych: PostgreSQL (produkcja) / SQLite (development)
- Dependency Injection: Wbudowany w .NET, użyty poprzez konstruktory
- UI Framework: XAML + CommunityToolkit.MVVM

---

### Diagram Architektury (Mermaid)

```mermaid
graph TD
    Client["👤 Klient / Kasjer"]
    App["🎬 App.xaml.cs<br/>Entry Point"]
    
    subgraph "Warstwa Prezentacji (WPF/XAML)"
        MainWindow["MainWindow<br/>UI Kasjera"]
        CustomerWindow["CustomerWindow<br/>UI Klienta"]
        LoginView["LoginView"]
        DashboardView["DashboardView"]
        MoviesView["MoviesView"]
        SeatSelectionView["SeatSelectionView"]
        TicketSummaryView["TicketSummaryView"]
    end
    
    subgraph "Warstwa Logiki Prezentacji (ViewModels)"
        MainVM["MainViewModel<br/>Orkiestrator"]
        LoginVM["LoginViewModel"]
        DashboardVM["DashboardViewModel<br/>Hub Nawigacji"]
        MoviesVM["MoviesViewModel<br/>Katalog"]
        SeatSelectionVM["SeatSelectionViewModel<br/>Mapa Miejsc"]
        TicketSummaryVM["TicketSummaryViewModel<br/>Koszyk & Finalizacja"]
        MovieDetailVM["MovieDetailViewModel"]
    end
    
    subgraph "Warstwa Biznesowa (Services)"
        AuthService["AuthenticationService<br/>Weryfikacja Loginu"]
        ScreenManager["ScreenManager<br/>Multi-Monitor"]
        ReportService["ReportSettingsService<br/>Analityka"]
        EmailService["EmailService<br/>Powiadomienia"]
        DbGenerator["DatabaseGenerator<br/>Inicjalizacja & Migracja"]
    end
    
    subgraph "Warstwa Danych (EF Core)"
        DbContext["CinemaDbContext<br/>Entity Framework"]
        Models["Modele Domeny<br/>Film, Screening, Ticket, itd."]
    end
    
    subgraph "Baza Danych"
        DB["PostgreSQL<br/>(Prod) / SQLite (Dev)"]
    end
    
    Client -->|Interakcja| MainWindow
    Client -->|Interakcja| CustomerWindow
    App -->|Inicjalizacja| MainVM
    App -->|Setup DB| DatabaseGenerator
    
    MainWindow -->|Data Binding| MainVM
    CustomerWindow -->|Data Binding| MainVM
    
    MainVM -->|Nawigacja| LoginVM
    MainVM -->|Nawigacja| DashboardVM
    DashboardVM -->|Nawigacja| MoviesVM
    DashboardVM -->|Nawigacja| SeatSelectionVM
    DashboardVM -->|Nawigacja| TicketSummaryVM
    
    LoginView -->|Binding| LoginVM
    DashboardView -->|Binding| DashboardVM
    MoviesView -->|Binding| MoviesVM
    SeatSelectionView -->|Binding| SeatSelectionVM
    TicketSummaryView -->|Binding| TicketSummaryVM
    MovieDetailVM -->|Szczegóły| MovieDetailViewModel
    
    LoginVM -->|Weryfikacja| AuthService
    SeatSelectionVM -->|Ładowanie| DbContext
    TicketSummaryVM -->|Zapis| DbContext
    DashboardVM -->|Raporty| ReportService
    
    AuthService -->|Query| DbContext
    DbContext -->|Entity Mapping| Models
    DbContext -->|ORM| DB
```

---

### Rola Frameworka MVVM i Wzorca Application Controller

#### MVVM w Kontekście Aplikacji

Architektura **MVVM (Model-View-ViewModel)** stanowi fundament separacji odpowiedzialności w projekcie:

| Komponent | Odpowiedzialność | Przykład |
|-----------|------------------|---------|
| **Model** | Encje biznesowe, logika domeny | `Film`, `Screening`, `Reservation`, `Ticket` |
| **View** | UI - XAML, renderowanie, binding | `MoviesView.xaml`, `SeatSelectionView.xaml` |
| **ViewModel** | Logika prezentacji, state management, commands | `MoviesViewModel`, `SeatSelectionViewModel` |

#### Implementacja: CommunityToolkit.MVVM

Projekt wykorzystuje **CommunityToolkit.Mvvm 8.4.0** do:

1. **ObservableObject** – Automatyczne implementowanie `INotifyPropertyChanged`
   ```csharp
   public partial class LoginViewModel : ViewModelBase
   {
       [ObservableProperty]
       private string _username = "";  // Automatycznie notifikuje UI o zmianach
   }
   ```

2. **RelayCommand** – Powiązanie poleceń UI z metodami
   ```csharp
   [RelayCommand]
   private async Task Login(object parameter) { ... }
   // Generuje automatycznie LoginCommand do bindingu w XAML
   ```

3. **ViewModelBase** – Zwykły bazowy ViewModel (core/ViewModelBase.cs) dziedziczący `ObservableObject`

#### Wzorzec Application Controller (Orchestration)

System implementuje wzorzec Application Controller poprzez hierarchię ViewModeli:

```
┌─────────────────────────────────────┐
│      MainViewModel (Orchestrator)   │
│  - Zarządza całym lifecycle'iem     │
│  - Synchronizuje dwa okna (Staff/   │
│    Customer)                        │
│  - Emituje zdarzenia (events)       │
└────────────┬──────────────────────────┘
             │
        ┌────┴─────────────────┐
        │                     │
    ┌───▼────────┐      ┌────▼────────┐
    │ LoginVM    │      │ DashboardVM │
    │(Punkt start)│      │(Navigation) │
    └────────────┘      └────┬────────┘
                             │
         ┌───────────────────┼──────────────────┐
         │                   │                  │
      ┌──▼──┐          ┌────▼──┐      ┌───────▼─┐
      │Movie│          │Schedule│     │Reports  │
      │     │          │        │     │         │
      └──┬──┘          └────────┘     └─────────┘
         │
      ┌──▼──────────────────┐
      │ MovieDetail         │──┐
      │ (ShowOnClick)       │  │
      └─────────────────────┘  │
              │                │
          ┌───▼────────────────▼─────┐
          │ SeatSelectionViewModel   │
          │ (Choose Hall Layout)     │
          └──────────────┬───────────┘
                         │
          ┌──────────────▼────────────┐
          │ TicketSummaryViewModel   │
          │ (Cart & Checkout)        │
          └──────────────────────────┘
```

**Kluczowe cechy wzorca:**

- **Event-Driven Communication:** ViewModele komunikują się poprzez zdarzenia (`LoginSuccess`, `ShowClientScheduleRequested`, `LogoutRequested`)
- **Unidirectional Data Flow:** MainViewModel kieruje nawigacją, ViewModele emitują żądania
- **Decoupling:** ViewModele nie wiedzą o sobie nawzajem, komunikują się poprzez MainViewModel
- **Dual-Display Synchronization:** Jedno MainViewModel obsługuje dwa okna, synchronizując stan

**Przykład:**
```csharp
// MainViewModel.cs
private void OnLoginSuccess(User user)
{
    _dashboardViewModel = new DashboardViewModel(_moviesViewModel, user);
    
    // Subskrybowanie zdarzeń z DashboardViewModel
    _dashboardViewModel.LogoutRequested += () => ShowLoginScreen();
    _dashboardViewModel.ShowClientScheduleRequested += StartClientScheduleTimer;
    _dashboardViewModel.PropertyChanged += Dashboard_PropertyChanged;
    
    CurrentView = _dashboardViewModel;  // Nawigacja do dashboard'a
}
```

---

### Przepływ Danych (Data Flow)

```
Użytkownik
    ↓
[Klik w UI - XAML Event]
    ↓
[View Event → ViewModel.Command/Method]
    ↓
[ViewModel: Modyfikacja State (ObservableProperty)]
    ↓
[INotifyPropertyChanged → UI Update (Data Binding)]
    ↓
[Jeśli konieczne: Database Access via DbContext]
    ↓
[EF Core ORM → PostgreSQL/SQLite]
    ↓
[Entity materialization → ViewModel cache]
    ↓
[UI Refresh via Data Binding]
```

---

## Specyfikacja Komponentów

### 1. Models (Domain Entities)

Modele reprezentują encje biznesowe, będące „Single Source of Truth" dla danych. Każdy model mapuje się 1:1 na tabelę w bazie danych (Entity Framework Convention).

#### Film.cs

**Odpowiedzialność:** Reprezentacja filmów w katalogu kinowego.

| Właściwość | Typ | Opis | Uwagi |
|-----------|-----|------|-------|
| `Id` | `int` | Primary Key | Auto-increment |
| `Title` | `string` | Tytuł filmu | [Required] |
| `Description` | `string` | Streszczenie | Do 1000 znaków |
| `Duration` | `int` | Czas trwania (minuty) | Default: 120 |
| `Genre` | `string` | Kategoria | "Drama", "Comedy", "Thriller" |
| `ReleaseDate` | `string` | Data premiery | Format: "dd.MM.yyyy" |
| `PosterPath` | `string` | Ścieżka do plakatu | Relatywna: "Resources/Images/film.jpg" |
| `ImdbRating` | `double` | Ocena (0.0-10.0) | Z IMDB |
| `AvailabilityDays` | `int` | Liczba dni dostępności | Do kiedy film jest w repertuarze |
| `DirectorId` | `int?` | Foreign Key | Nullable – wiele filmów bez reżysera |
| `Director` | `Director?` | Navigation Property | EF Core relationship |
| `IsActive` | `bool` | Flaga aktywności | Default: true |
| `PosterUri` | `string` | Computed Property | Zwraca pełną ścieżkę lub placeholder.png |

**Relacje:**
- **1:N** z `Director` (N filmów na 1 reżysera)
- **1:N** z `Screening` (film ma wiele seansów)

**Walidacja (Data Annotations):**
```csharp
[Table("Films")]
[Key] public int Id { get; set; }
[Required] public string Title { get; set; }
```

---

#### Screening.cs

**Odpowiedzialność:** Reprezentacja konkretnego seansu (film + sala + czas).

| Właściwość | Typ | Opis | Uwagi |
|-----------|-----|------|-------|
| `Id` | `int` | Primary Key | |
| `MovieId` | `int?` | FK → Film | Nullable |
| `HallId` | `int?` | FK → Hall | Nullable |
| `Start` | `DateTime` | Czas rozpoczęcia | Przechowuje full datetime |
| `Price` | `decimal` | Cena biletu | Default: 25.00 |
| `Movie` | `Film?` | Navigation Property | |
| `Hall` | `Hall?` | Navigation Property | |
| `TimeString` | `string` | Computed | "HH:mm" (np. "19:30") |
| `DateString` | `string` | Computed | "dd.MM.yyyy" (np. "15.03.2026") |

**Dziedziczy:** `ObservableObject` (reactivity dla UI)

**Relacje:**
- **N:1** z `Film` (wiele seansów dla jednego filmu)
- **N:1** z `Hall` (wiele seansów w jednej sali)
- **1:N** z `Reservation`

---

#### Hall.cs

**Odpowiedzialność:** Reprezentacja sali kinowej i jej layoutu.

| Właściwość | Typ | Opis | Uwagi |
|-----------|-----|------|-------|
| `Id` | `int` | Primary Key | |
| `Name` | `string` | Nazwa sali | "Sala 1", "IMAX", "4DX" |
| `Rows` | `int` | Liczba rzędów | Np. 12 |
| `SeatsPerRow` | `int` | Miejsc na rząd | Np. 18 |
| `EntrancePosition` | `string` | Pozycja wejścia | "Right", "Left", "Back" |

**Dziedziczy:** `ObservableObject`

**Bezpośrednie użycie:** Skalkulowanie mapy sali
- Całkowita pojemność = `Rows × SeatsPerRow`
- Pozycja wejścia wpływa na orientację wyświetlania

---

#### Seat.cs

**Odpowiedzialność:** V-model reprezentujący pojedyncze miejsce w sali (część UI).

| Właściwość | Typ | Opis | Uwagi |
|-----------|-----|------|-------|
| `Row` | `int` | Numer rzędu | 1-based |
| `Number` | `int` | Numer miejsca | 1-based |
| `Status` | `SeatStatus` (enum) | Stan | Available, Taken, Selected, Editing |
| `DisplayName` | `string` | Formatted "R: X \| M: Y" | Computed property |

**Enum `SeatStatus`:**
```csharp
Available  // Wolne, można zarezerwować
Taken      // Zajęte (sprzedane)
Selected   // Zaznaczone przez użytkownika
Editing    // Edycja typu biletu
```

**Dziedziczy:** `ObservableObject`

**Nota:** `Seat` **NIE jest** mapowany na tabelę w DB. Jest czystym V-modelem, pochodzącym z `Hall`. Tworzy się dynamicznie w `SeatSelectionView`.

---

#### Reservation.cs

**Odpowiedzialność:** Rezerwacja biletów dla konkretnego seansu przez klienta.

| Właściwość | Typ | Opis | Uwagi |
|-----------|-----|------|-------|
| `Id` | `int` | Primary Key | |
| `RowVersion` | `byte[]?` | Timestamp | Optimistic Concurrency Control |
| `ScreeningId` | `int` | FK → Screening | |
| `Screening` | `Screening?` | Navigation Property | |
| `CustomerEmail` | `string` | Email klienta | |
| `CustomerName` | `string` | Imię i nazwisko | [Required] |
| `CreatedAt` | `string` | Data/czas rezerwacji | Format: "dd.MM.yyyy HH:mm" |
| `Tickets` | `List<Ticket>` | Navigation Property | 1:N relacja |
| `Status` | `string` | Status rezerwacji | "Active", "Cancelled", "Completed" |

**Concurrency Control:**
- `[Timestamp]` implementuje optimistic locking
- Baza automatycznie aktualizuje `RowVersion` przy każdej modyfikacji
- Uniemożliwia lost updates w przypadku konkurencyjnych edycji

---

#### Ticket.cs

**Odpowiedzialność:** Pojedynczy bilet (konkretne miejsce + typ + cena).

| Właściwość | Typ | Opis | Uwagi |
|-----------|-----|------|-------|
| `Id` | `int` | Primary Key | |
| `RowVersion` | `byte[]?` | Timestamp | Optimistic Concurrency Control |
| `ReservationId` | `int` | FK → Reservation | |
| `Reservation` | `Reservation?` | Navigation Property | |
| `Row` | `int` | Numer rzędu | Reference do miejsca |
| `SeatNumber` | `int` | Numer miejsca | Reference do miejsca |
| `TransactionNumber` | `string` | Unikalny numer biletu | Np. "TKT-20260315-000042" |
| `IssuedAt` | `string` | Data wystawienia | [Required] |
| `Status` | `string` | Status biletu | "Active", "Used", "Cancelled" |
| `PricePaid` | `decimal` | Cena zapłacona | Po uwzględnieniu rabatów |
| `TicketType` | `string` | Typ biletu | "Normalny", "Ulgowy", "Student" |

**Relacje:**
- **N:1** z `Reservation`
- Implicit reference do `Hall.Seat` poprzez `Row` + `SeatNumber`

**Bezpieczeństwo:**
- `TransactionNumber` – unikatowy identyfikator do weryfikacji i śledzenia
- `RowVersion` – ochrona przed konkurencyjnymi modyfikacjami

---

#### User.cs

**Odpowiedzialność:** Reprezentacja użytkownika/pracownika systemu.

| Właściwość | Typ | Opis | Uwagi |
|-----------|-----|------|-------|
| `Id` | `int` | Primary Key | |
| `Login` | `string` | Nazwa użytkownika | [Required] |
| `PasswordHash` | `string` | Hash hasła | [Required] – nigdy nie raw password |
| `Role` | `string` | Rola użytkownika | "Admin", "Cashier" |
| `FullName` | `string` | Pełne imię i nazwisko | |

**Role Access Control:**
- **Admin:** Pełny dostęp do wszystkich funkcji (filmy, sale, raporty, użytkownicy)
- **Cashier:** Dostęp do rezerwacji, sprzedaży biletów, raportów sprzedaży

**Nieważne:**
- Bieżąca implementacja przechowuje hasło w czystej postaci (`PasswordHash` to misleading naming)
- **TODO:** Implementacja bcrypt/PBKDF2 haszing

---

#### Dodatkowe Modele

| Model | Opis |
|-------|------|
| `Director` | Reżyser filmów (1:N z Film) |
| `PromoCode` | Kod promocyjny/rabatowy (IsPercentage flag, DiscountValue) |
| `TicketTypeDefinition` | Definicja typu biletu (PriceMultiplier lub FixedPrice) |
| `Advertisement` | Reklama wyświetlana w UI (Title, ImagePath, IsActive) |

---

### 2. ViewModels (Presentation Logic)

ViewModele zarządzają logiką prezentacji, state management, i komunikacją z Models/Services. Wszystkie ViewModele dziedziczą z `ViewModelBase` (które rozszerza `ObservableObject`).

#### MainViewModel.cs – Orchestrator

**Rola:** Główny orkiestrator aplikacji, zarządza dwoma oknamiami (Staff + Customer).

**Właściwości:**
```csharp
[ObservableProperty] private object _currentView;      // Staff UI
[ObservableProperty] private object _clientView;       // Customer UI
```

**Zdarzenia:**
- `LoginSuccess` – Zalogowano użytkownika
- `LogoutRequested` – Wylogowanie
- `ShowClientScheduleRequested` – Pokaż harmonogram klientowi

**Metody:**
```csharp
private void ShowLoginScreen()           // Wyświetl ekran logowania
private void OnLoginSuccess(User user)   // Obsługa logowania
private void Dashboard_PropertyChanged    // Sync między widokami
private void StartClientScheduleTimer    // Timer do pokazania harmonogramu
```

**Kluczowe logika:**
- Inicjalizuje `MoviesViewModel` raz dla całej aplikacji (shared state)
- Subskrybuje zdarzenia z `DashboardViewModel` do synchronizacji
- Timer 30-sekundowy do wyświetlania harmonogramu dla klienta

---

#### LoginViewModel.cs – Authentication

**Rola:** Obsługa ekranu logowania, weryfikacja kredencjałów.

**Właściwości:**
```csharp
[ObservableProperty] private string _username = "";
[ObservableProperty] private string _errorMessage = "";
```

**Commands:**
```csharp
[RelayCommand]
private async Task Login(object parameter)  // Pobierz PasswordBox, waliduj
```

**Mechanizm:**
1. Pobiera login i hasło z UI
2. Query do `CinemaDbContext` – szukaj użytkownika
3. Porównaj hasło (TODO: haszowanie)
4. Jeśli sukces → emit `LoginSuccess` event
5. MainViewModel obsługuje event i nawiguje do DashboardViewModel

---

#### DashboardViewModel.cs – Navigation Hub

**Rola:** Główny hub nawigacji dla kasjera. Zarządza przechodzeniem między widokami (Movies, Schedule, Reports, Tickets).

**Właściwości:**
```csharp
[ObservableProperty] private object _currentContent;
[ObservableProperty] private User? _currentUser;
public string UserNameDisplay => $"{CurrentUser?.FullName} ({CurrentUser?.Role})";
```

**Zdarzenia:**
```csharp
public event Action? LogoutRequested;
public event Action? ShowClientScheduleRequested;
```

**Commands (RelayCommand):**
```csharp
void NavigateToMovies()          // Pokaż listę filmów
void NavigateToSchedule()        // Pokaż harmonogram seansów
void NavigateToReports()         // Pokaż raporty sprzedaży
void GoToTicketManagement()      // Edycja/zwrot biletów
void ShowClientSchedule()        // Pokaż harmonogram klientowi
```

**Architektura:**
- Każdy command tworzy nowy ViewModel i przypisuje do `CurrentContent`
- `CurrentContent` jest Bindowany w XAML do `ContentControl`
- WPF automatycznie wybiera template (DataTemplate) dla każdego VM

---

#### MoviesViewModel.cs – Catalog

**Rola:** Zarządzanie katalogiem filmów dla kasjera i klienta. Filtrowanie, wyszukiwanie, gatunek.

**Właściwości:**
```csharp
[ObservableProperty] private ObservableCollection<Film> _movies;
[ObservableProperty] private ObservableCollection<Film> _recommendedMovies;
[ObservableProperty] private ObservableCollection<GenreItem> _genres;
[ObservableProperty] private string _searchText = "";
[ObservableProperty] private bool _isLoading;
[ObservableProperty] private Film? _selectedMovie;
```

**Reklamy (Advertisement System):**
```csharp
[ObservableProperty] private string _currentAdText;
private DispatcherTimer _adTimer;  // Timer rotacji reklam
```

**Metody:**
```csharp
private async void LoadMoviesAsync()       // Load from DB
private void LoadAds()                     // Wczytaj reklamy
private void FilterMovies()                // Apply search filter
private void StartAdTimer()                // Start ad rotation timer
```

**Logika filtrowania:**
- Jeśli `SearchText` pusty → wyświetl wszystkie
- Inaczej → LINQ WHERE (case-insensitive title search)
- Synchronizacja między `_movies` (UI) i `_allFilmsCache` (cache)

---

#### MovieDetailViewModel.cs – Detail View

**Rola:** Wyświetlanie szczegółów konkretnego filmu i jego seansów.

**Właściwości:**
```csharp
[ObservableProperty] private Film _selectedFilm;
[ObservableProperty] private ObservableCollection<Screening> _filmScreenings;
```

**Commands:**
```csharp
void GoBack()                   // Powrót do listy filmów
void BuyTicket(Screening)       // Przejdź do wyboru miejsc
```

**Ładowanie seansów:**
```csharp
private void LoadScreenings()
{
    using (var context = new CinemaDbContext())
    {
        var list = context.Screenings
            .Include(s => s.Hall)
            .Include(s => s.Movie)
            .Where(s => s.MovieId == SelectedFilm.Id && s.Start > DateTime.Now)
            .OrderBy(s => s.Start)
            .Take(10)  // Następne 10 seansów
            .ToList();
        
        FilmScreenings = new ObservableCollection<Screening>(list);
    }
}
```

---

#### SeatSelectionViewModel.cs – Seat Map

**Rola:** Wybór miejsc w sali. Dynamicznie generuje mapę sali na bazie `Hall` (Rows × SeatsPerRow).

**Właściwości:**
```csharp
[ObservableProperty] private Screening _currentScreening;
[ObservableProperty] private int _rowsCount;
[ObservableProperty] private int _columnsCount;
[ObservableProperty] private string _summaryText = "Wybierz miejsca";
[ObservableProperty] private bool _isEditMode;  // true = edycja istniejącego biletu
public ObservableCollection<Seat> Seats { get; set; }
public ObservableCollection<string> RowLabels { get; set; }
```

**Kluczowe operacje:**

1. **Generowanie sali:**
   ```csharp
   private void GenerateCinemaHall()
   {
       var layout = new List<Seat>();
       for (int row = 1; row <= RowsCount; row++)
       {
           for (int seat = 1; seat <= ColumnsCount; seat++)
           {
               layout.Add(new Seat { Row = row, Number = seat, Status = SeatStatus.Available });
           }
       }
       Seats = new ObservableCollection<Seat>(layout);
   }
   ```

2. **Ładowanie zajętych miejsc:**
   ```csharp
   private void LoadReservedSeats()
   {
       using (var context = new CinemaDbContext())
       {
           var takenSeats = context.Tickets
               .Include(t => t.Reservation)
               .Where(t => t.Reservation.ScreeningId == _currentScreening.Id &&
                          t.Reservation.Status == "Active")
               .ToList();
           
           foreach (var ticket in takenSeats)
           {
               var seat = Seats.FirstOrDefault(s => s.Row == ticket.Row && s.Number == ticket.SeatNumber);
               if (seat != null) seat.Status = SeatStatus.Taken;
           }
       }
   }
   ```

3. **Logika wyboru:**
   - Użytkownik klika na dostępne miejsce
   - Status zmienia się na `Selected`
   - `SummaryText` się aktualizuje

4. **Tryb edycji:**
   - Jeśli `_ticketToEdit != null` → pozwól zmienić miejsce dla istniejącego biletu
   - Zmień status poprzedniego miejsca z `Taken` na `Available`

**Tryb wsadowej edycji:** Obsługuje zmianę ceny/typu dla wybranych biletów.

---

#### TicketSummaryViewModel.cs – Checkout

**Rola:** Podsumowanie rezerwacji, zarządzanie koszykiem, finalizacja płatności.

**Klasy pomocnicze:**
```csharp
public partial class TicketCartItem : ObservableObject
{
    public Seat Seat { get; }
    [ObservableProperty] private TicketTypeDefinition _selectedType;
    [ObservableProperty] private decimal _price;
    
    // Zmiana typu aktualizuje cenę
    partial void OnSelectedTypeChanged(TicketTypeDefinition value)
    {
        Price = _screening.Price * (decimal)value.PriceMultiplier;
    }
}
```

**Właściwości:**
```csharp
[ObservableProperty] private ObservableCollection<TicketCartItem> _cartItems;
[ObservableProperty] private List<TicketTypeDefinition> _availableTicketTypes;
[ObservableProperty] private decimal _totalPrice;
[ObservableProperty] private string _appliedPromoCode = "";
[ObservableProperty] private decimal _discountAmount;
```

**Finalizacja rezerwacji:**
```csharp
[RelayCommand]
private async Task ConfirmReservation()
{
    using (var context = new CinemaDbContext())
    {
        var reservation = new Reservation
        {
            ScreeningId = _screening.Id,
            CustomerEmail = _customerEmail,
            CustomerName = _customerName,
            CreatedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
            Status = "Active",
            Tickets = _cartItems.Select(item => new Ticket
            {
                Row = item.Seat.Row,
                SeatNumber = item.Seat.Number,
                TransactionNumber = GenerateTransactionNumber(),
                IssuedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                TicketType = item.SelectedType.Name,
                PricePaid = item.Price,
                Status = "Active"
            }).ToList()
        };
        
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();  // Optimistic concurrency: RowVersion check
    }
    
    // Generuj i wyślij PDF
    await GenerateAndEmailPDF();
}
```

**Generowanie PDF:**
- Używa biblioteki **QuestPDF** do tworzenia dokumentu
- Dołącza kody QR dla każdego biletu (`QRCoder`)
- Wysyła email do klienta (`EmailService`)

---

### 3. Views (Presentation Layer)

ViewModele są bindowane do Views poprzez XAML (Data Binding). Każda View jest plikiem `.xaml` + `.xaml.cs`.

| ViewName | ViewModel | Opis |
|----------|-----------|------|
| `LoginView.xaml` | `LoginViewModel` | Logowanie użytkownika (kasjer) |
| `DashboardView.xaml` | `DashboardViewModel` | Główny dashboard – nawigacja, menu |
| `MoviesView.xaml` | `MoviesViewModel` | Lista filmów, wyszukiwanie, gatunek |
| `MovieDetailView.xaml` | `MovieDetailViewModel` | Szczegóły filmu + lista seansów |
| `ScheduleView.xaml` | `ScheduleViewModel` | Harmonogram seansów (dla kasjera) |
| `SeatSelectionView.xaml` | `SeatSelectionViewModel` | Interaktywna mapa sali |
| `TicketSummaryView.xaml` | `TicketSummaryViewModel` | Koszt, rabaty, podsumowanie |
| `ReportsView.xaml` | `ReportsViewModel` | Raporty sprzedaży i statystyki |
| `TicketManagementView.xaml` | `TicketManagementViewModel` | Edycja/zwrot biletów |

**Mechanizm Tab Navigation:**
```xaml
<!-- DashboardView.xaml -->
<TabControl ItemsSource="{Binding NavigationTabs}" SelectedIndex="{Binding SelectedTabIndex}">
    <TabControl.ContentTemplate>
        <DataTemplate>
            <ContentControl Content="{Binding}" />
        </DataTemplate>
    </TabControl.ContentTemplate>
</TabControl>
```

WPF automatycznie wybiera odpowiedni DataTemplate na bazie typu ViewModelu:
```xaml
<DataTemplate DataType="{x:Type local:MoviesViewModel}">
    <local:MoviesView />
</DataTemplate>
```

---

## Warstwa Danych i Persystencja

### PostgreSQL – Fundament Bazy Danych

System **rezerwacji biletów kinowych** opiera się na **PostgreSQL** jako głównym systemie zarządzania bazą danych dla środowiska produkcyjnego. PostgreSQL został wybrana ze względu na:

- **Niezawodność** – Open-source, enterprise-grade reliability
- **Performance** – Optymalna wydajność dla dużych zbiorów danych rezerwacji
- **Skalowanie** – Wsparcie dla connection pooling (Npgsql) i wielu równoczesnych użytkowników
- **ACID Compliance** – Pełne wsparcie transakcji i data integrity (krytyczne dla operacji finansowych)
- **Optymistyczne Blokowanie** – Obsługa RowVersion (timestamp) do detecji konfliktów współbieżności
- **Cloud Support** – Łatwe wdrażanie na Heroku, Railway, Render, AWS RDS, Azure PostgreSQL

#### Konfiguracja Połączenia PostgreSQL

```csharp
// Services/CinemaDbContext.cs – OnConfiguring()
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    // Production: pobierz connection string ze zmiennej środowiskowej
    string connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
        ?? "Host=localhost;Database=cinema_db;Username=postgres;Password=postgres;Port=5432";
    
    // Konfiguracja Npgsql dla PostgreSQL
    optionsBuilder.UseNpgsql(
        connectionString,
        options => options
            .EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), new[] { "42P01" })
            .CommandTimeout(30)
    );
}
```

**Parametry PostgreSQL:**
- `Host`: Adres serwera bazy danych
- `Database`: Nazwa bazy (`cinema_db`)
- `Username`: Użytkownik PostgreSQL (`postgres`)
- `Port`: Port PostgreSQL (domyślnie `5432`)
- **Connection Pooling**: Automatycznie włączone w Npgsql – zarządza pulą połączeń
- **Retry Policy**: 3 próby z 10-sekundowym opóźnieniem przy tymczasowych błędach sieci
- **Command Timeout**: 30 sekund dla każdego zapytania SQL

**Development vs Production:**
- **Localhost** (development): `Host=localhost;Database=cinema_db;Username=postgres;Password=postgres;Port=5432`
- **Cloud** (production): `postgresql://user:password@host.com:5432/cinema_db` (via $env:DATABASE_URL)
- **Fallback** (tylko jeśli PostgreSQL niedostępny): SQLite (kino.db)

---

### Schemat Bazy Danych

#### Tabele i Relacje

```
┌──────────────────────────────────────────────────────────────┐
│                    CINEMA SYSTEM DATABASE                     │
└──────────────────────────────────────────────────────────────┘

┌─────────────────────┐
│      DIRECTORS      │
├─────────────────────┤
│ PK: Id (int)        │
│ Name (string)       │◄────────────┐
└─────────────────────┘             │
                                    │ (1:N)
┌─────────────────────┐             │
│       FILMS         │             │
├─────────────────────┤             │
│ PK: Id (int)        │             │
│ Title (string)      │             │
│ Description         │             │
│ Duration (int)      │             │
│ Genre (string)      │             │
│ ReleaseDate         │             │
│ PosterPath          │             │
│ ImdbRating          │             │
│ AvailabilityDays    │             │
│ DirectorId (FK)     ├─────────────┘
│ IsActive (bool)     │
└────────┬────────────┘
         │
         │ (1:N)
         │
└────────▼────────────────────────┐
                                  │
┌─────────────────────────────┐   │
│         HALLS               │   │
├─────────────────────────────┤   │
│ PK: Id (int)                │   │
│ Name (string)               │   │
│ Rows (int)                  │   │
│ SeatsPerRow (int)           │   │
│ EntrancePosition (string)   │   │
└────────┬────────────────────┘   │
         │                        │
         │ (1:N)                  │
         │                        │
    ┌────▼────────────────────────▼────┐
    │      SCREENINGS (Seanse)         │
    ├──────────────────────────────────┤
    │ PK: Id (int)                     │
    │ MovieId (FK → Films)             │
    │ HallId (FK → Halls)              │
    │ Start (DateTime)                 │
    │ Price (decimal)                  │
    │ Duration (string)                │
    └────┬─────────────────────────────┘
         │
         │ (1:N)
         │
    ┌────▼──────────────────────────────┐
    │      RESERVATIONS (Rezerwacje)    │
    ├───────────────────────────────────┤
    │ PK: Id (int)                      │
    │ ScreeningId (FK → Screenings)    │
    │ CustomerEmail (string)            │
    │ CustomerName (string)             │
    │ CreatedAt (DateTime)              │
    │ Status (string)                   │
    │ RowVersion (byte[], timestamp)    │
    └────┬──────────────────────────────┘
         │
         │ (1:N)
         │
    ┌────▼──────────────────────────────┐
    │        TICKETS (Bilety)           │
    ├───────────────────────────────────┤
    │ PK: Id (int)                      │
    │ ReservationId (FK)                │
    │ Row (int)                         │
    │ SeatNumber (int)                  │
    │ TransactionNumber (string)        │ [UNIQUE]
    │ IssuedAt (DateTime)               │
    │ Status (string)                   │
    │ PricePaid (decimal)               │
    │ TicketType (string)               │
    │ RowVersion (byte[], timestamp)    │
    └───────────────────────────────────┘

┌───────────────────────────────────────┐
│         USERS (Użytkownicy)           │
├───────────────────────────────────────┤
│ PK: Id (int)                          │
│ Login (string)                        │
│ PasswordHash (string)                 │
│ Role (string: "Admin"|"Cashier")      │
│ FullName (string)                     │
└───────────────────────────────────────┘

┌──────────────────────────────────────────┐
│   PROMO_CODES (Kody Promocyjne)          │
├──────────────────────────────────────────┤
│ PK: Id (int)                             │
│ Code (string) [UNIQUE]                   │
│ DiscountValue (double)                   │
│ IsPercentage (bool)                      │
│ IsActive (bool)                          │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│  TICKET_TYPE_DEFINITIONS (Typy Biletów)  │
├──────────────────────────────────────────┤
│ PK: Id (int)                             │
│ Name (string): "Normalny"|"Ulgowy"...    │
│ PriceMultiplier (double)                 │
│ FixedPrice (double?, nullable)           │
│ IsActive (bool)                          │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│      ADVERTISEMENTS (Reklamy)            │
├──────────────────────────────────────────┤
│ PK: Id (int)                             │
│ Title (string)                           │
│ ImagePath (string)                       │
│ Content (string)                         │
│ IsActive (bool)                          │
├──────────────────────────────────────────┤
│ DisplayPosition ("Sidebar"|"Popup")      │
└──────────────────────────────────────────┘
```

---

### Diagram ERD (Mermaid)

```mermaid
erDiagram
    DIRECTORS ||--o{ FILMS : "1:N"
    FILMS ||--o{ SCREENINGS : "1:N"
    HALLS ||--o{ SCREENINGS : "1:N"
    SCREENINGS ||--o{ RESERVATIONS : "1:N"
    RESERVATIONS ||--o{ TICKETS : "1:N"
    TICKET_TYPES ||--o{ TICKETS : references
    PROMO_CODES ||--o{ RESERVATIONS : references

    DIRECTORS {
        int id PK
        string name
    }

    FILMS {
        int id PK
        string title
        string description
        int duration
        string genre
        string releaseDate
        string posterPath
        double imdbRating
        int availabilityDays
        int directorId FK
        bool isActive
    }

    HALLS {
        int id PK
        string name
        int rows
        int seatsPerRow
        string entrancePosition
    }

    SCREENINGS {
        int id PK
        int movieId FK
        int hallId FK
        datetime start
        decimal price
        string duration
    }

    RESERVATIONS {
        int id PK
        int screeningId FK
        string customerEmail
        string customerName
        datetime createdAt
        string status
        byte[] rowVersion "Concurrency"
    }

    TICKETS {
        int id PK
        int reservationId FK
        int row
        int seatNumber
        string transactionNumber "UNIQUE"
        datetime issuedAt
        string status
        decimal pricePaid
        string ticketType
        byte[] rowVersion "Concurrency"
    }

    USERS {
        int id PK
        string login
        string passwordHash
        string role
        string fullName
    }

    PROMO_CODES {
        int id PK
        string code "UNIQUE"
        double discountValue
        bool isPercentage
        bool isActive
    }

    TICKET_TYPES {
        int id PK
        string name
        double priceMultiplier
        double fixedPrice
        bool isActive
    }

    ADVERTISEMENTS {
        int id PK
        string title
        string imagePath
        string content
        bool isActive
        string displayPosition
    }
```

---

### CinemaDbContext.cs – Entity Framework Core Configuration

**Rola:** Centralna klasa do zarządzania połączeniem z bazą danych i mapowania encji.

```csharp
public class CinemaDbContext : DbContext
{
    // DbSets – replikacja tabel
    public DbSet<Film> Films { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Director> Directors { get; set; }
    public DbSet<Hall> Halls { get; set; }
    public DbSet<Screening> Screenings { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<PromoCode> PromoCodes { get; set; }
    public DbSet<TicketTypeDefinition> TicketTypes { get; set; }
    public DbSet<Advertisement> Advertisements { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Production: Pobierz z zmiennej środowiskowej (DATABASE_URL)
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Development: domyślny localhost PostgreSQL
            connectionString = "Host=localhost;Database=cinema_db;Username=postgres;Password=postgres;Port=5432";
        }
        
        // Konfiguracja PostgreSQL
        optionsBuilder.UseNpgsql(
            connectionString,
            options => options
                .EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelayMilliseconds: 10000,
                    errorCodesToAdd: new[] { "42P01" }  // Retry na specific errors
                )
                .CommandTimeout(30)
        );

        #if DEBUG
            optionsBuilder.EnableSensitiveDataLogging(true);  // Wyświetlaj wartości w logach
        #endif
    }
}
```

**Konfiguracja połączenia:**
- **Production:** Zmienna `DATABASE_URL` (Heroku, Railway, Render)
- **Development:** `localhost:5432` PostgreSQL
- **Retry Policy:** 3 próby z 10-sekundowym opóźnieniem
- **Command Timeout:** 30 sekund

---

### Migration Strategy

#### Entity Framework Core Migrations

Projekt zawiera dwie migracje:

1. **20260106155819_InitialCreate.cs** – Tabele główne (Films, Screenings, Halls, Directors, Users)
2. **20260106184120_AddHalls.cs** – Dodatkowe tabele (Halls enhancements lub inne)

**Obsługa migracji:**

```csharp
// Database initialization
public static void GenerateFinalDatabase()
{
    using (var context = new CinemaDbContext())
    {
        context.Database.Migrate();  // Apply all pending migrations
    }
}
```

**Wywoływane w `App.xaml.cs` OnStartup()** – pewność, że baza jest zawsze aktualna.

---

### Data Access Patterns

#### Pattern 1: Query with Include (Eager Loading)

```csharp
// Pobierz film z jego reżyserem, bez N+1 problemu
var film = context.Films
    .Include(f => f.Director)
    .FirstOrDefault(f => f.Id == filmId);
```

#### Pattern 2: Filtering and Ordering

```csharp
// Pobierz aktywne filmy, posortowane alfabetycznie
var activeMovies = context.Films
    .Where(f => f.IsActive)
    .OrderBy(f => f.Title)
    .ToList();
```

#### Pattern 3: Optimistic Concurrency Control

```csharp
// Rezerwacja ma RowVersion [Timestamp]
var reservation = context.Reservations.FirstOrDefault(r => r.Id == id);
reservation.Status = "Cancelled";

try
{
    await context.SaveChangesAsync();  // DbConcurrencyException jeśli zmienione przez inny proces
}
catch (DbUpdateConcurrencyException ex)
{
    // Obsłuż conflict – reload i retry lub notify user
}
```

#### Pattern 4: Transaction Handling

```csharp
using (var transaction = context.Database.BeginTransaction())
{
    try
    {
        var reservation = new Reservation { ... };
        context.Reservations.Add(reservation);

        var tickets = reservation.Tickets;  // Dodaj bilety
        context.Tickets.AddRange(tickets);

        await context.SaveChangesAsync();
        transaction.Commit();
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        throw;
    }
}
```

---

### Relationships (Relacja Models)

| Relacja | Type | Cascade Delete | Opis |
|---------|------|-----------------|------|
| Film → Director | N:1 | Yes | Usunięcie reżysera usuwa jego filmy |
| Screening → Film | N:1 | Yes | Usunięcie filmu usuwa jego seanse |
| Screening → Hall | N:1 | Yes | Usunięcie sali usuwa jej seanse |
| Reservation → Screening | N:1 | Yes | Usunięcie seansu usuwa rezerwacje |
| Ticket → Reservation | N:1 | Yes | Usunięcie rezerwacji usuwa bilety |

---

## Przepływy Procesowe

### Proces Rezerwacji – Critical User Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                   PROCES REZERWACJI BILETU                        │
└──────────────────────────────────────────────────────────────────┘

START: Klient wchodzi do kina
│
├─ [1] EKRAN LOGOWANIA (LoginView)
│   └─ LoginViewModel weryfikuje login/hasło
│   └─ AuthenticationService querry'uje Users z DB
│   └─ Jeśli OK → LoginSuccess event
│
├─ [2] DASHBOARD (DashboardView)
│   └─ MainViewModel odbiera LoginSuccess
│   └─ Tworzy DashboardViewModel z zalogowanym User
│   └─ CurrentContent = DashboardView
│
├─ [3] LISTA FILMÓW (MoviesView)
│   └─ Kasjer kliknie "Filmy" lub "Nowa Rezerwacja"
│   └─ DashboardViewModel.NavigateToMovies()
│   └─ CurrentContent = MoviesViewModel
│   └─ LoadMoviesAsync() → fetch Films z DB (Include Director, Filter IsActive)
│   └─ Wyświetla listę filmów z posterami
│
├─ [4] SZCZEGÓŁY FILMU (MovieDetailView) [OPCJONALNE]
│   └─ Użytkownik klika na film
│   └─ DashboardViewModel.CurrentContent = new MovieDetailViewModel(film)
│   └─ LoadScreenings() → fetch Screenings dla tego Film ID
│   │   └─ Include Hall, Include Movie
│   │   └─ Where Start > DateTime.Now (tylko seansy w przyszłości)
│   │   └─ OrderBy Start, Take(10)
│   └─ Wyświetla szczegóły: title, description, duration, seansy
│
├─ [5] WYBÓR SEANSU & PRZEJŚCIE DO WYBORU MIEJSC
│   └─ Użytkownik klika "Kupić bilet" dla wybranego seansu
│   └─ DashboardViewModel.CurrentContent = new SeatSelectionViewModel(screening)
│
├─ [6] MAPA SALI (SeatSelectionView)
│   ├─ SeatSelectionViewModel inicjalizacja:
│   │  ├─ GenerateCinemaHall() – Create (Rows × SeatsPerRow) Seat objects
│   │  │  └─ Status = Available (default)
│   │  └─ LoadReservedSeats() – Query DB:
│   │     ├─ context.Tickets.Include(t => t.Reservation)
│   │     ├─ Where(t => t.Reservation.ScreeningId == currentScreening.Id)
│   │     ├─ Where(t => t.Reservation.Status == "Active")
│   │     └─ Dla każdego znalezionego ticketa: Set Seat.Status = Taken
│   │
│   ├─ UI Rendering:
│   │  └─ GridPanel wyświetla Seats
│   │  └─ Each Seat → Button z color binding:
│   │     ├─ Available → Green
│   │     ├─ Taken → Red
│   │     ├─ Selected → Blue/Highlighted
│   │
│   ├─ User Interaction:
│   │  ├─ Kliknie Available seat → Status = Selected
│   │  ├─ SummaryText aktualizuje: "Wybrane: 2 miejsca, cena: 50.00 zł"
│   │  └─ Może multi-select
│   │
│   └─ Next: Klik "Dalej" → Przejście do TicketSummaryViewModel
│
├─ [7] PODSUMOWANIE & FINALIZACJA (TicketSummaryView)
│   ├─ TicketSummaryViewModel inicjalizacja:
│   │  ├─ CartItems = Selected Seats (Seat → TicketCartItem mapping)
│   │  ├─ LoadAvailableTicketTypes() – Query DB:
│   │  │  └─ context.TicketTypes.Where(t => t.IsActive)
│   │  └─ Default TicketType = first entry
│   │
│   ├─ UI Wyświetla:
│   │  ├─ Koszyk: [ Seat(R:5 M:12) | Typ: Normalny | Cena: 25.00 ]
│   │  ├─ Dla każdego biletu: dropdown select Type, cena przelicza się
│   │  ├─ Total: 50.00 zł
│   │  └─ Input: CustomerEmail, CustomerName, PromoCode (optional)
│   │
│   ├─ Promo Code Logic:
│   │  ├─ User wpisuje kod (np. "SUMMER20")
│   │  ├─ Klik "Aplicuj kod"
│   │  ├─ Query DB: context.PromoCodes.FirstOrDefault(p => p.Code == input && p.IsActive)
│   │  ├─ Jeśli found:
│   │  │  ├─ If IsPercentage=true: Discount = Total × DiscountValue / 100
│   │  │  ├─ Else: Discount = DiscountValue (flat)
│   │  │  └─ FinalPrice = Total - Discount
│   │  └─ Jeśli nie znaleziony: "Kod nie ważny"
│   │
│   └─ Next: Klik "Potwierdź rezerwację"
│
├─ [8] DATABASE COMMIT – TRANSAKCJA
│   ├─ var reservation = new Reservation
│   │  ├─ ScreeningId = _screening.Id
│   │  ├─ CustomerEmail = input
│   │  ├─ CustomerName = input
│   │  ├─ CreatedAt = DateTime.Now.ToString(...)
│   │  ├─ Status = "Active"
│   │  ├─ RowVersion = null (EF automatycznie inicjalizuje)
│   │  │
│   │  └─ Tickets = CartItems.Select(item => new Ticket
│   │     ├─ ReservationId = reservation.Id (FK)
│   │     ├─ Row = item.Seat.Row
│   │     ├─ SeatNumber = item.Seat.Number
│   │     ├─ TransactionNumber = GenerateUniqueNumber()  // TKT-20260315-000042
│   │     ├─ IssuedAt = DateTime.Now
│   │     ├─ Status = "Active"
│   │     ├─ PricePaid = item.Price
│   │     ├─ TicketType = item.SelectedType.Name
│   │     └─ RowVersion = null
│   │
│   ├─ context.Reservations.Add(reservation)
│   ├─ context.SaveChangesAsync()  ← INSERTS:
│   │  ├─ 1 row to Reservations
│   │  └─ N rows to Tickets (optimistic concurrency check on all)
│   │
│   └─ Success / DbConcurrencyException handling
│
├─ [9] PDF GENERATION & EXPORT
│   ├─ GeneratePdfTickets()
│   │  ├─ Using QuestPDF library
│   │  ├─ Dla każdego Ticket:
│   │  │  ├─ Title: "Bilet Kinowy"
│   │  │  ├─ Film: reservation.Screening.Movie.Title
│   │  │  ├─ Sala: reservation.Screening.Hall.Name
│   │  │  ├─ Data/Czas: reservation.Screening.Start
│   │  │  ├─ Miejsce: Ticket.Row + Ticket.SeatNumber
│   │  │  ├─ Cena: Ticket.PricePaid
│   │  │  ├─ QR Code: GenerateQR(ticket.TransactionNumber)
│   │  │  └─ TransactionNumber: ticket.TransactionNumber
│   │  │
│   │  └─ Save to Desktop: C:\Users\...\Desktop\Tickets_[Date].pdf
│   │
│   └─ EmailService.SendAsync(email, pdf)  ← OPCJONALNE: wysłanie mailem
│
├─ [10] CONFIRMATION SCREEN
│   └─ Wyświetl potwierdzenie: "Rezerwacja udana! Bilet został zapisany."
│   └─ Offer: "Drukuj bilet" lub "Zamknij"
│
└─ END: Powrót do ekranu wyboru filmów (MoviesView)
```

---

### Diagram Sekwencji – Reservation Booking (Mermaid)

```mermaid
sequenceDiagram
    actor User as Użytkownik/Kasjer
    participant UI as SeatSelectionView
    participant VM as SeatSelectionVM
    participant DB as EF Core Context
    participant SQL as PostgreSQL

    User->>UI: Klik "Dalej" (potwierdzenie wyboru miejsc)
    UI->>VM: ConfirmSeatSelection()
    
    VM->>DB: Query Screenings (Include Hall, Movie)
    DB->>SQL: SELECT * FROM Screenings WHERE Id = ?
    SQL-->>DB: Obiekt Screening
    DB-->>VM: Screening wczytany
    
    VM->>DB: Query Tickets - sprawdzenie zarezerwowanych miejsc
    DB->>SQL: SELECT * FROM Tickets JOIN Reservations WHERE ScreeningId = ?
    SQL-->>DB: Lista Ticketów
    DB-->>VM: Zarezerwowane miejsca oznaczone jako Zajęte
    
    Note over VM: Generuj layout sali & oznacz zarezerwowane miejsca
    
    User->>UI: Klik na wolne miejsce (np. R:5 M:12)
    UI->>VM: OnSeatClicked(seat)
    Note over VM: seat.Status = Wybrane
    
    User->>UI: Multi-select - więcej miejsc
    UI->>VM: Wiele zdarzeń OnSeatClicked
    VM->>VM: Akumuluj wybrane miejsca na liście lokalnej
    
    User->>UI: Klik "Dalej" → Idź do Podsumowania
    UI->>VM: NavigateToSummary()
    VM->>UI: Wyświetl TicketSummaryView
    
    Note over UI,VM: Inicjalizacja TicketSummaryViewModel
    
    UI->>VM: TicketSummaryViewModel(selectedSeats, screening)
    VM->>DB: Query TicketTypeDefinitions WHERE IsActive = true
    DB->>SQL: SELECT * FROM TicketTypes WHERE IsActive = true
    SQL-->>DB: Dostępne Typy Biletów
    DB-->>VM: TicketTypes wczytane
    
    User->>UI: Wpisz Email klienta, Imię i Nazwisko
    User->>UI: Wybierz typ biletu na miejsce (dropdown)
    User->>UI: [Opcja] Wpisz Kod Promocyjny
    
    User->>UI: Klik "Zastosuj Kod"
    VM->>DB: Query PromoCodes WHERE Code = ? AND IsActive = true
    DB->>SQL: SELECT * FROM PromoCodes WHERE Code = ? AND IsActive = true
    SQL-->>DB: Kod Promocyjny (jeśli istnieje)
    DB-->>VM: Kod Promocyjny wczytany
    
    Note over VM: Oblicz rabat & Cenę Końcową
    
    User->>UI: Klik "Potwierdź Rezerwację"
    UI->>VM: ConfirmReservation()
    
    Note over VM: Skonstruuj obiekty Reservation + Tickets
    
    VM->>DB: Dodaj Reservation
    VM->>DB: Dodaj Tickets (kolekcja)
    DB->>DB: Mapuj do kontekstu + zastosuj timestampy
    
    VM->>DB: SaveChangesAsync()
    DB->>SQL: BEGIN TRANSACTION
    SQL->>SQL: INSERT INTO Reservations (...)
    SQL->>SQL: INSERT INTO Tickets (...) x N
    SQL->>SQL: COMMIT TRANSACTION
    SQL-->>DB: Sukces / RowVersion przypisany
    
    DB-->>VM: SaveChangesAsync ukończone
    
    Note over VM: Generuj PDF Biletu
    
    VM->>VM: GeneratePDFTickets(reservation)
    Note over VM: QuestPDF + QRCoder
    
    VM->>VM: SavePDFToDesktop()
    
    Note over VM: [Opcja] Wyślij Email
    
    VM->>UI: Pokaż Wiadomość Sukcesu
    UI->>User: Wyświetl: "Rezerwacja udana!"
    
    User->>UI: Klik "Zamknij" lub "Nowa Rezerwacja"
    UI->>VM: Reset lub NavigateBack()
```

---

## Zależności i Rozwój

### Dependency Injection Mapping

Projekt wykorzystuje wbudowany DI container .NET (Microsoft.Extensions.DependencyInjection), ale konfiguracja manualna poprzez konstruktory.

| Interface | Implementation | Lifecycle | Użycie |
|-----------|---|---|---|
| `CinemaDbContext` | `DbContext` | Transient | Każde query tworzy nową instancję (using statement) |
| `AuthenticationService` | Custom | Singleton | Logowanie, instancja globalna |
| `ScreenManager` | Custom | Singleton | Zarządzanie ekranami |
| `EmailService` | Custom | Singleton | Wysyłanie emaili |
| `ReportSettingsService` | Custom | Singleton | Konfiguracja raportów |

**Przykład (brak IoC containerera, manual DI):**

```csharp
// App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    // Manual instantiation (no DI container framework)
    var authService = new AuthenticationService();
    var screenManager = new ScreenManager();
    
    var mainVM = new MainViewModel();  // Contains dependencies
    var loginVM = new LoginViewModel(authService);  // Pass dependencies
}
```

**TODO:** Zamigrowanie na `Microsoft.Extensions.DependencyInjection` dla full DI support.

---

### Obsługa Błędów

#### Try-Catch Patterns

```csharp
// Pattern 1: Login Error Handling
try
{
    var user = await Task.Run(() =>
    {
        using var context = new CinemaDbContext();
        return context.Users.FirstOrDefault(u => u.Login == Username && u.PasswordHash == password);
    });

    if (user != null)
        LoginSuccess?.Invoke(user);
    else
        ErrorMessage = "Błędne dane logowania.";
}
catch (Exception ex)
{
    ErrorMessage = "Błąd: " + ex.Message;
}
```

```csharp
// Pattern 2: Database Concurrency
try
{
    await context.SaveChangesAsync();
}
catch (DbConcurrencyException)
{
    // RowVersion mismatch – bilet zmieniony przez inny proces
    MessageBox.Show("Bilet został zmieniony przez innego użytkownika. Spróbuj ponownie.");
    // Reload entity
    await context.Entry(ticket).ReloadAsync();
}
catch (DbUpdateException ex)
{
    // SQL constraint violation, foreign key, etc.
    MessageBox.Show($"Błąd bazy danych: {ex.Message}");
}
```

#### Walidacja (Frontend)

```csharp
// LoginViewModel
if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
{
    ErrorMessage = "Podaj login i hasło.";
    return;
}
```

#### ViewModels – Validation

```csharp
// TicketSummaryViewModel
if (string.IsNullOrWhiteSpace(_customerEmail))
{
    MessageBox.Show("Podaj email.");
    return;
}

if (!_customerEmail.Contains("@"))
{
    MessageBox.Show("Email niepoprawny.");
    return;
}
```

---

### Logging Architecture

Projekt nie zawiera zentralnego loggera. **TODO:** Add Serilog/NLog.

**Propozycja:**
```csharp
// Add Serilog
using Serilog;

public static void Main()
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.Console()
        .CreateLogger();
    
    try
    {
        Log.Information("Application started");
        // ...
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application terminated unexpectedly");
    }
    finally
    {
        Log.CloseAndFlush();
    }
}
```

---

### Development Guide – Dodaj Nową Funkcjonalność

#### Scenariusz: Dodaj "Ulubione Filmy" Feature

**Krok 1: Dodaj Model**

Utwórz `Models/UserFavorite.cs`:
```csharp
[Table("UserFavorites")]
public class UserFavorite
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public int FilmId { get; set; }
    
    [ForeignKey("UserId")]
    public User? User { get; set; }
    
    [ForeignKey("FilmId")]
    public Film? Film { get; set; }
    
    public DateTime AddedAt { get; set; }
}
```

**Krok 2: Update DbContext**

```csharp
public class CinemaDbContext : DbContext
{
    public DbSet<UserFavorite> UserFavorites { get; set; }
    // ...
}
```

**Krok 3: Migration**

```bash
dotnet ef migrations add AddUserFavorites
dotnet ef database update
```

**Krok 4: Dodaj ViewModel Logic**

```csharp
public partial class MoviesViewModel : ViewModelBase
{
    [RelayCommand]
    private async Task AddToFavorites(Film film)
    {
        using var context = new CinemaDbContext();
        var favorite = new UserFavorite
        {
            UserId = _currentUser.Id,
            FilmId = film.Id,
            AddedAt = DateTime.Now
        };
        context.UserFavorites.Add(favorite);
        await context.SaveChangesAsync();
        
        MessageBox.Show("Dodano do ulubionych!");
    }
}
```

**Krok 5: Update View (XAML binding)**

```xaml
<Button Command="{Binding AddToFavoritesCommand}" CommandParameter="{Binding SelectedMovie}">
    ❤️ Dodaj do ulubionych
</Button>
```

**Krok 6: Test**
- Zaloguj się
- Wybierz film
- Klik "Dodaj do ulubionych"
- Sprawdź DB: `SELECT * FROM UserFavorites`

---

### Architecture Best Practices

#### 1. SOLID Principles

| Principle | Implementation |
|-----------|---|
| **S**ingle Responsibility | Każdy ViewModel ma jedną odpowiedzialność (MoviesVM = katalog, SeatSelectionVM = mapa) |
| **O**pen/Closed | Services otwarte na rozszerzenie (add EmailService, SMSService) |
| **L**iskov Substitution | ViewModele zamienialne dzięki ViewModelBase |
| **I**nterface Segregation | Małe interfaces (AuthenticationService, ScreenManager) |
| **D**ependency Inversion | Dependency injection poprzez constructory |

#### 2. DRY (Don't Repeat Yourself)

- **Shared ViewModel:**
  ```csharp
  _moviesViewModel = new MoviesViewModel();  // Jedna instancja dla kasjera i klienta
  ```

- **Reusable Services:**
  ```csharp
  var authService = new AuthenticationService();  // Jeden serwis dla logowania
  ```

#### 3. Clean Code

- **Naming:** `ConfirmReservation`, `LoadReservedSeats`, `GenerateCinemaHall`
- **Methods:** Małe, focused, max 50 linii
- **Comments:** Dokumentują "dlaczego", nie "co" (code is self-explanatory)

#### 4. Task-Based Asynchronous Pattern (TAP)

```csharp
// ✓ Poprawnie
[RelayCommand]
private async Task Login(object parameter)
{
    var user = await Task.Run(() => { ... });
}

// ✗ Sync over async – antipattern
public void Login()
{
    var user = LoginAsync().Result;  // Blocks!
}
```

---

### Performance Optimization

#### 1. EF Core Query Optimization

```csharp
// Load Screenings with related data (prevent N+1)
var screenings = context.Screenings
    .Include(s => s.Movie)      // Load Movie
    .Include(s => s.Hall)       // Load Hall
    .Where(s => s.Start > DateTime.Now)
    .ToList();  // ✓ Single query with JOINs
```

#### 2. Caching

```csharp
// Cache film list
private List<Film> _allFilmsCache = new();

private void LoadMoviesAsync()
{
    if (_allFilmsCache.Count > 0)
        return;  // Use cache
    
    // Fetch from DB only once
    _allFilmsCache = context.Films.ToList();
}
```

#### 3. Connection Pooling

```csharp
// PostgreSQL connection pooling (enabled by default in Npgsql)
.UseNpgsql(connectionString, options =>
    options.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10))
)
```

---

### Security Considerations

| Issue | Current | Recommendation |
|-------|---------|---|
| **Password Hashing** | Plain text (TODO) | Use BCrypt/PBKDF2 |
| **SQL Injection** | EF Core (safe) | ✓ No raw SQL |
| **Authentication** | Basic (login/pass) | Add JWT, OAuth2 |
| **Authorization** | Role-based (Admin/Cashier) | ✓ Implemented |
| **Data Encryption** | None | Add TLS/HTTPS + DB encryption |
| **CORS** | N/A (Desktop) | N/A for WPF |

---

## Podsumowanie Architektury

### Kluczowe Komponenty

1. **Prezentacja (WPF/XAML):** MainWindow, CustomerWindow, widoki (Views)
2. **Logika (ViewModels):** MVVM orchestration via CommunityToolkit.Mvvm
3. **Modele (Domains):** 10 entities (Film, Screening, Ticket, itp.)
4. **Dane (EF Core):** PostgreSQL/SQLite via DbContext
5. **Serwisy:** Auth, Email, Screen Management

### Przepływ

```
User Input (XAML) 
  ↓
ViewModel (RelayCommand/ObservableProperty)
  ↓
Service/DbContext (Business Logic)
  ↓
Database (Persistence)
  ↓
ViewModel (Property notification)
  ↓
UI Update (Data Binding)
```

### Rozszerzeń Przyszłości

- [ ] Full Dependency Injection (Microsoft.Extensions.DependencyInjection)
- [ ] Centralized Logging (Serilog)
- [ ] Password Hashing (BCrypt)
- [ ] JWT Authentication
- [ ] Web API (ASP.NET Core microservice)
- [ ] Mobile App (Xamarin/MAUI)
- [ ] Advanced Analytics & Reporting
- [ ] Machine Learning (Recommendation engine)

---

## Kontakt & Dokumentacja

**Autor Dokumentacji:** Senior Technical Architect  
**Data:** Marzec 2026  
**Framework:** .NET 10 + WPF  
**Status:** Production Ready  

Dokumentacja stanowi **Single Source of Truth** dla całego projektu. Każda zmiana w architekturze:
1. Zmień kod
2. Zaktualizuj tę dokumentację
3. Sprawdzenie (code review + docs review)

---

**Koniec dokumentacji technicznej.**
