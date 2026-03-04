# Developer Guide – Rozszerzanie Systemu Rezerwacji Biletów

## 📖 Spis Treści

1. [Dodawanie Nowych Funkcjonalności](#dodawanie-nowych-funkcjonalności)
2. [ViewModele i Commands](#viewmodele-i-commands)
3. [Praca z Bazą Danych](#praca-z-bazą-danych)
4. [Patterns & Best Practices](#patterns--best-practices)
5. [Debugging & Testing](#debugging--testing)
6. [Performance Tips](#performance-tips)

---

## Dodawanie Nowych Funkcjonalności

### Scenariusz 1: Dodaj Nowy Model (np. "CustomerReview")

#### Krok 1: Utwórz Model Klasy

```csharp
// Models/CustomerReview.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystem.Desktop.Models
{
    /// <summary>
    /// Opinia klienta o seansie/filmie.
    /// </summary>
    [Table("CustomerReviews")]
    public class CustomerReview
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }  // FK reference

        [ForeignKey("TicketId")]
        public Ticket? Ticket { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public int Rating { get; set; }  // 1-5 stars

        public string ReviewText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsApproved { get; set; } = false;
    }
}
```

#### Krok 2: Update DbContext

```csharp
// Services/CinemaDbContext.cs
public class CinemaDbContext : DbContext
{
    public DbSet<CustomerReview> CustomerReviews { get; set; }  // ADD THIS
    
    // ... rest of DbSets
}
```

#### Krok 3: Utwórz Migrację EF

```bash
dotnet ef migrations add AddCustomerReviews
dotnet ef database update
```

Generated migration:
```csharp
// Migrations/[Date]_AddCustomerReviews.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "CustomerReviews",
        columns: table => new
        {
            Id = table.Column<int>(nullable: false)
                .Annotation("Sqlite:Autoincrement", true),
            TicketId = table.Column<int>(nullable: false),
            CustomerName = table.Column<string>(nullable: false),
            Rating = table.Column<int>(nullable: false),
            ReviewText = table.Column<string>(nullable: true),
            CreatedAt = table.Column<DateTime>(nullable: false),
            IsApproved = table.Column<bool>(nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_CustomerReviews", x => x.Id);
            table.ForeignKey(
                name: "FK_CustomerReviews_Tickets",
                column: x => x.TicketId,
                principalTable: "Tickets",
                principalColumn: "Id"
            );
        }
    );
}
```

---

### Scenariusz 2: Dodaj Nowy ViewModel & View

#### Krok 1: Utwórz ViewModel

```csharp
// ViewModels/ReviewsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CinemaSystem.Desktop.Core;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CinemaSystem.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel do przeglądania i dodawania opinii o filmach.
    /// </summary>
    public partial class ReviewsViewModel : ViewModelBase
    {
        /// <summary>
        /// Lista wszystkich zatwierdonych opinii.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CustomerReview> _reviews = new();

        /// <summary>
        /// Nowego opinię do dodania.
        /// </summary>
        [ObservableProperty]
        private string _newReviewText = "";

        /// <summary>
        /// Ocena (1-5) do nowej opinii.
        /// </summary>
        [ObservableProperty]
        private int _newReviewRating = 5;

        /// <summary>
        /// Czy wczytywanie opinii trwa.
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        public ReviewsViewModel()
        {
            LoadReviewsAsync();
        }

        /// <summary>
        /// Ładuje wszystkie zatwierdzone opinie z bazy.
        /// </summary>
        private async void LoadReviewsAsync()
        {
            IsLoading = true;

            await Task.Run(() =>
            {
                using (var context = new CinemaDbContext())
                {
                    var reviews = context.CustomerReviews
                        .Where(r => r.IsApproved)
                        .OrderByDescending(r => r.CreatedAt)
                        .ToList();

                    Reviews = new ObservableCollection<CustomerReview>(reviews);
                }
            });

            IsLoading = false;
        }

        /// <summary>
        /// Dodaje nową opinię (oczekuje na zatwierdzenie admina).
        /// </summary>
        [RelayCommand]
        private async Task SubmitReview()
        {
            if (string.IsNullOrWhiteSpace(NewReviewText) || NewReviewRating < 1 || NewReviewRating > 5)
            {
                MessageBox.Show("Wpisz opinię i wybierz ocenę (1-5).", "Błąd");
                return;
            }

            await Task.Run(() =>
            {
                using (var context = new CinemaDbContext())
                {
                    var review = new CustomerReview
                    {
                        // TicketId = current ticket (you need to pass it)
                        CustomerName = "Anonymous",
                        Rating = NewReviewRating,
                        ReviewText = NewReviewText,
                        CreatedAt = System.DateTime.Now,
                        IsApproved = false
                    };

                    context.CustomerReviews.Add(review);
                    context.SaveChanges();
                }
            });

            MessageBox.Show("Dzięki za opinię! Czeka na zatwierdzenie.", "Sukces");
            NewReviewText = "";
            NewReviewRating = 5;
            LoadReviewsAsync();
        }
    }
}
```

#### Krok 2: Utwórz View (XAML)

```xaml
<!-- Views/ReviewsView.xaml -->
<UserControl x:Class="CinemaSystem.Desktop.Views.ReviewsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Background="White">
        <StackPanel Margin="20">
            <!-- Title -->
            <TextBlock Text="Opinie Klientów" FontSize="24" FontWeight="Bold" Margin="0,0,0,20"/>

            <!-- Loading Indicator -->
            <ProgressBar IsIndeterminate="True" Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}" Height="4" Margin="0,0,0,10"/>

            <!-- Reviews List -->
            <ItemsControl ItemsSource="{Binding Reviews}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border BorderBrush="LightGray" BorderThickness="1" Padding="10" Margin="0,0,0,10" CornerRadius="4">
                            <StackPanel>
                                <TextBlock Text="{Binding CustomerName}" FontWeight="Bold"/>
                                <TextBlock Text="{Binding Rating, StringFormat='Ocena: {0}/5'}" Foreground="Orange"/>
                                <TextBlock Text="{Binding ReviewText}" TextWrapping="Wrap" Margin="0,5,0,0"/>
                                <TextBlock Text="{Binding CreatedAt, StringFormat='d MMM yyyy'}" FontSize="11" Foreground="Gray"/>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>

            <!-- Add Review Section -->
            <Border BorderBrush="LightBlue" BorderThickness="1" Padding="10" CornerRadius="4" Margin="0,20,0,0">
                <StackPanel>
                    <TextBlock Text="Dodaj Opinię" FontSize="14" FontWeight="Bold" Margin="0,0,0,10"/>
                    <TextBlock Text="Ocena:" Margin="0,0,0,5"/>
                    <Slider Value="{Binding NewReviewRating}" Minimum="1" Maximum="5" TickFrequency="1" IsSnapToTickEnabled="True"/>
                    
                    <TextBlock Text="Opinia:" Margin="0,10,0,5"/>
                    <TextBox Text="{Binding NewReviewText}" TextWrapping="Wrap" Height="80" AcceptsReturn="True"/>
                    
                    <Button Command="{Binding SubmitReviewCommand}" Content="Prześlij" Margin="0,10,0,0" Padding="10"/>
                </StackPanel>
            </Border>
        </StackPanel>
    </Grid>
</UserControl>
```

#### Krok 3: Code-Behind (rejestracja DataTemplate)

```csharp
// Views/ReviewsView.xaml.cs
using System.Windows.Controls;

namespace CinemaSystem.Desktop.Views
{
    public partial class ReviewsView : UserControl
    {
        public ReviewsView()
        {
            InitializeComponent();
        }
    }
}
```

#### Krok 4: Rejestruj DataTemplate w App.xaml

```xaml
<!-- App.xaml -->
<Application>
    <Application.Resources>
        <ResourceDictionary>
            <!-- MVVM Template Mappings -->
            <DataTemplate DataType="{x:Type local:ReviewsViewModel}">
                <local:ReviewsView />
            </DataTemplate>
            
            <!-- ... existing templates ... -->
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

#### Krok 5: Dodaj do Nawigacji (DashboardViewModel)

```csharp
// ViewModels/DashboardViewModel.cs
[RelayCommand]
public void NavigateToReviews() => CurrentContent = new ReviewsViewModel();
```

---

## ViewModele i Commands

### Pattern: RelayCommand

```csharp
// ✓ POPRAWNIE – Async Task
[RelayCommand]
private async Task LoadData()
{
    await Task.Run(() => { /* expensive operation */ });
}

// ✓ POPRAWNIE – Sync Void
[RelayCommand]
private void Navigate(string view)
{
    CurrentContent = view;
}

// ✗ NIE RÓB – async void (antipattern)
[RelayCommand]
private async void BadPattern()  // NEVER
{
    await Task.Delay(1000);
}
```

### Pattern: ObservableProperty

```csharp
// ✓ POPRAWNIE – Auto-generates property
[ObservableProperty]
private string _username = "";

// Generated code (behind scenes):
public string Username
{
    get => _username;
    set => SetProperty(ref _username, value);
}

// Możliwość triggera na zmianę
partial void OnUsernameChanged(string value)
{
    MessageBox.Show($"Username changed to: {value}");
}
```

### Pattern: Property Binding

```xaml
<!-- View Binding to ViewModel -->
<TextBox Text="{Binding CurrentUser.FullName, Mode=TwoWay, UpdateTrigger=PropertyChanged}" />
<Button Command="{Binding LoginCommand}" Content="Zaloguj"/>
<ItemsControl ItemsSource="{Binding Movies}" />

<!-- Value Converter -->
<TextBlock Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}" />
```

---

## Praca z Bazą Danych

### ⭐ PostgreSQL – Produkcyjny System Bazy Danych

System **opiera się na PostgreSQL** jako głównym systemem zarządzania bazą danych dla środowiska produkcyjnego. 

**Ważne Informacje:**
- **Produkcja:** PostgreSQL 12+ (obowiązkowy)
- **Development:** PostgreSQL (localhost) – domyślnie
- **Fallback:** SQLite (kino.db) – tylko jeśli PostgreSQL niedostępny

**CinemaDbContext.cs OnConfiguring():**
```csharp
string connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? "Host=localhost;Database=cinema_db;Username=postgres;Password=postgres;Port=5432";

optionsBuilder.UseNpgsql(connectionString, options => options
    .EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), new[] { "42P01" })
    .CommandTimeout(30)
);
```

**Operacje EF Core (identyczne dla PostgreSQL i SQLite):**
```bash
# Create migration
dotnet ef migrations add AddMyFeature

# Apply migration
dotnet ef database update

# Revert migration
dotnet ef database update PreviousMigration
```

**Różnice Migracji:**
- **PostgreSQL**: Identity autoincrement natively (nie potrzeba adnotacji)
- **SQLite**: Wymaga `.Annotation("Sqlite:Autoincrement", true)`

---

### Pattern 1: Query z Eager Loading

```csharp
// ✓ DOBRZE – Load related entities
var screenings = context.Screenings
    .Include(s => s.Movie)       // Eager load Movie
    .Include(s => s.Hall)        // Eager load Hall
    .Include(s => s.Reservations)  // Eager load Reservations
    .Where(s => s.Start > DateTime.Now)
    .ToList();

// ✗ ŹLE – N+1 Problem
var screenings = context.Screenings.ToList();
foreach (var s in screenings)
{
    var movie = context.Movies.First(m => m.Id == s.MovieId);  // Extra query for each screening!
}
```

### Pattern 2: Filtering & Sorting

```csharp
// Multi-criteria filtering
var films = context.Films
    .Where(f => f.IsActive)
    .Where(f => f.Genre == "Drama")
    .Where(f => f.ImdbRating >= 7.0)
    .OrderByDescending(f => f.ImdbRating)
    .ThenBy(f => f.Title)
    .ToList();
```

### Pattern 3: Transaction Management

```csharp
using (var transaction = context.Database.BeginTransaction())
{
    try
    {
        // Add multiple entities
        var reservation = new Reservation { /* ... */ };
        context.Reservations.Add(reservation);

        foreach (var seat in selectedSeats)
        {
            var ticket = new Ticket { ReservationId = reservation.Id, /* ... */ };
            context.Tickets.Add(ticket);
        }

        context.SaveChanges();
        transaction.Commit();
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        MessageBox.Show($"Błąd: {ex.Message}");
    }
}
```

### Pattern 4: Optimistic Concurrency

```csharp
try
{
    var ticket = context.Tickets.First(t => t.Id == ticketId);
    ticket.Status = "Used";
    
    // If RowVersion changed by another process → DbConcurrencyException
    context.SaveChanges();
}
catch (DbConcurrencyException)
{
    // Reload & retry
    context.Entry(ticket).Reload();
    MessageBox.Show("Bilet zmieniony przez innego użytkownika. Spróbuj ponownie.");
}
```

### Migration Guide

```bash
# Create new migration
dotnet ef migrations add AddNewTable

# Review generated migration (check Migrations/[date]_AddNewTable.cs)

# Apply to database
dotnet ef database update

# Rollback last migration
dotnet ef database update PreviousMigration

# Remove latest migration (not applied)
dotnet ef migrations remove

# Script migration (for deployment)
dotnet ef migrations script --output migration.sql
```

---

## Patterns & Best Practices

### Pattern: MVVM Command Binding

```csharp
// ViewModel
public partial class MyViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _inputText = "";

    [RelayCommand]
    private void ProcessInput()
    {
        MessageBox.Show($"Input: {InputText}");
        InputText = "";  // Clear after processing
    }
}

// XAML
<TextBox Text="{Binding InputText, Mode=TwoWay}" />
<Button Command="{Binding ProcessInputCommand}" Content="Process" />
```

### Pattern: Master-Detail Navigation

```csharp
// MoviesViewModel
[RelayCommand]
private void SelectMovie(Film film)
{
    _dashboardViewModel.CurrentContent = new MovieDetailViewModel(film, _dashboardViewModel);
}

// MovieDetailViewModel
public partial class MovieDetailViewModel : ViewModelBase
{
    private Film _selectedFilm;
    private DashboardViewModel _dashboard;

    public MovieDetailViewModel(Film film, DashboardViewModel dashboard)
    {
        _selectedFilm = film;
        _dashboard = dashboard;
    }

    [RelayCommand]
    private void GoBack()
    {
        _dashboard.CurrentContent = new MoviesViewModel();
    }
}
```

### Pattern: Loading State with UI Feedback

```csharp
[ObservableProperty]
private bool _isLoading;

[RelayCommand]
private async Task LoadData()
{
    IsLoading = true;
    try
    {
        await Task.Delay(2000);  // Simulated work
        // Load from DB
    }
    finally
    {
        IsLoading = false;  // Always set false
    }
}
```

```xaml
<!-- Show spinner while loading -->
<ProgressBar IsIndeterminate="True" 
             Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}"/>

<!-- Disable button during load -->
<Button Command="{Binding LoadDataCommand}" 
        IsEnabled="{Binding IsLoading, Converter={StaticResource InverseBoolConverter}}"/>
```

### Pattern: Error Handling in ViewModel

```csharp
[ObservableProperty]
private string _errorMessage = "";

[RelayCommand]
private async Task Login()
{
    ErrorMessage = "";  // Clear previous errors

    if (string.IsNullOrWhiteSpace(Username))
    {
        ErrorMessage = "Wpisz login.";
        return;
    }

    try
    {
        var user = await AuthenticateAsync();
        if (user != null)
            LoginSuccess?.Invoke(user);
        else
            ErrorMessage = "Błęde dane logowania.";
    }
    catch (Exception ex)
    {
        ErrorMessage = $"Błąd: {ex.Message}";
    }
}
```

```xaml
<!-- Display error -->
<TextBlock Text="{Binding ErrorMessage}" Foreground="Red" TextWrapping="Wrap" />
```

---

## Debugging & Testing

### Debugging Tips

#### 1. Enable EF Core Logging

```csharp
// In OnConfiguring
#if DEBUG
    optionsBuilder
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging();
#endif
```

#### 2. Breakpoints in ViewModel

```csharp
[RelayCommand]
private void MyCommand()
{
    System.Diagnostics.Debugger.Break();  // Stop here during debugging
    
    var x = CalculateValue();
    MessageBox.Show($"Value: {x}");
}
```

#### 3. Console Logging

```csharp
System.Diagnostics.Debug.WriteLine($"DEBUG: Films loaded: {Films.Count}");
System.Diagnostics.Trace.WriteLine("TRACE: Entering LoadMovies");
```

### Unit Testing (TODO – Add xUnit/NUnit)

```csharp
// Tests/ViewModels/LoginViewModelTests.cs (Example)
using Xunit;

public class LoginViewModelTests
{
    [Fact]
    public async Task Login_WithValidCredentials_InvokesLoginSuccess()
    {
        // Arrange
        var viewModel = new LoginViewModel();
        bool eventFired = false;
        viewModel.LoginSuccess += (user) => eventFired = true;

        // Act
        viewModel.Username = "admin";
        await viewModel.LoginCommand.ExecuteAsync("password");

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShowsError()
    {
        // Arrange
        var viewModel = new LoginViewModel();

        // Act
        viewModel.Username = "invalid";
        await viewModel.LoginCommand.ExecuteAsync("wrong");

        // Assert
        Assert.NotEmpty(viewModel.ErrorMessage);
    }
}
```

---

## Performance Tips

### 1. Caching

```csharp
private List<Film> _filmCache;

private void LoadFilms()
{
    if (_filmCache is not null)
        return;  // Use cache

    using (var context = new CinemaDbContext())
    {
        _filmCache = context.Films.ToList();
    }
}

// Invalidate cache when data changes
private void InvalidateCache()
{
    _filmCache = null;
}
```

### 2. Lazy Loading (Prevent)

```csharp
// ✗ SLOW – Loads related entity on access (N+1)
var films = context.Films.ToList();
foreach (var film in films)
{
    var director = film.Director;  // Extra query each time!
}

// ✓ FAST – Eager load
var films = context.Films.Include(f => f.Director).ToList();
foreach (var film in films)
{
    var director = film.Director;  // Already loaded
}
```

### 3. Pagination

```csharp
// Load only 20 items per page
int pageSize = 20;
int pageNumber = 1;

var films = context.Films
    .OrderBy(f => f.Title)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToList();
```

### 4. AsNoTracking for Read-Only

```csharp
// For queries where you don't modify data
var films = context.Films
    .AsNoTracking()  // Don't track changes
    .ToList();

// Faster, less memory
```

### 5. Connection Pooling

```csharp
// Already enabled in OnConfiguring
.UseNpgsql(connectionString, options =>
    options.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10))
)
```

---

## Code Style Guide

### Naming Conventions

```csharp
// ✓ Public properties – PascalCase
public string FirstName { get; set; }

// ✓ Private fields – camelCase with underscore
private string _firstName;

// ✓ Private methods – PascalCase
private void CalculateTotal() { }

// ✓ Constants – UPPER_CASE
private const int MAX_RETRIES = 3;

// ✓ Async methods – end with Async
private async Task LoadDataAsync() { }

// ✓ Event handlers – start with On
private void OnButtonClick() { }

// ✓ Boolean properties – start with Is/Has/Can
public bool IsLoading { get; set; }
public bool HasReviews { get; set; }
public bool CanDelete { get; set; }
```

### Comments

```csharp
// ✓ Explain WHY (not WHAT – code explains that)
// Cache is invalidated when new films are added
_filmCache = null;

// ✓ XML documentation for public members
/// <summary>
/// Loads films from database asynchronously.
/// </summary>
/// <param name="genreFilter">Optional genre to filter by</param>
/// <returns>List of films</returns>
private async Task<List<Film>> LoadFilmsAsync(string genreFilter = null)
{
    // ...
}

// ✗ Don't comment obvious code
int x = 5;  // Set x to 5 (unnecessary!)
```

---

## Checklist: Dodawanie Nowej Feature

- [ ] Create/Update Model classes
- [ ] Add DbSet to CinemaDbContext
- [ ] Create EF migration: `dotnet ef migrations add ...`
- [ ] Apply migration: `dotnet ef database update`
- [ ] Create ViewModel (inherit ViewModelBase)
- [ ] Create View.xaml + View.xaml.cs
- [ ] Add DataTemplate in App.xaml
- [ ] Add navigation command in parent ViewModel
- [ ] Update menu/buttons in parent View
- [ ] Test basic functionality
- [ ] Write tests (Unit + Integration)
- [ ] Add documentation comments
- [ ] Code review
- [ ] Merge to main branch

---

**Happy Coding! 🚀**

For more details, refer to [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md)
