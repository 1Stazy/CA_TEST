# Setup PostgreSQL dla Cinema System

## Wymagania

- PostgreSQL 12+ (rekomendacja: 15+)
- pgAdmin 4 (interfejs webowy - opcjonalnie)
- DBeaver lub DataGrip (GUI - opcjonalnie)

---

## INSTALACJA (Windows)

### 1. Pobierz i zainstaluj PostgreSQL

1. Przejdź na https://www.postgresql.org/download/windows/
2. Pobierz installer dla Windows (Binary)
3. Uruchom installer:
   ```
   PostgreSQL-15.x-1-windows-x64.exe
   ```
4. Podczas instalacji:
   - **Default port:** 5432 (nie zmieniaj)
   - **Superuser password:** `postgres` (zmień na silne hasło!)
   - **Locale:** Polish (Polska)

### 2. Zweryfikuj instalację

Otwórz PowerShell i wykonaj:

```powershell
psql --version
# Powinno wyświetlić: psql (PostgreSQL) 15.x
```

---

## KONFIGURACJA BAZY DANYCH

### 1. Zaloguj się do PostgreSQL

```powershell
# Zaloguj jako superuser
psql -U postgres
```

Wpisz hasło które podałeś podczas instalacji.

### 2. Utwórz bazę dla Cinema System

Wewnątrz psql prompt (`postgres=#`):

```sql
-- 1. Utwórz bazę danych
CREATE DATABASE cinema_db
  ENCODING 'UTF8'
  TEMPLATE template0
  LC_COLLATE 'pl_PL.UTF-8'
  LC_CTYPE 'pl_PL.UTF-8';

-- 2. Utwórz użytkownika (aplikacji)
CREATE USER cinema_user WITH ENCRYPTED PASSWORD 'CinemaPass123!';

-- 3. Przyznaj uprawnienia
GRANT CONNECT ON DATABASE cinema_db TO cinema_user;
GRANT USAGE ON SCHEMA public TO cinema_user;
GRANT CREATE ON SCHEMA public TO cinema_user;

-- 4. Wyjdź z psql
\q
```

### 3. Weryfikacja

```powershell
# Zaloguj się nową kredencjałem
psql -U cinema_user -d cinema_db -h localhost

# Powinno wyświetlić prompt:
# cinema_db=>

# Wyjdź
\q
```

---

## MIGRACJA BAZY DANYCH

### 1. Zaktualizuj connection string w aplikacji

**Lokacja:** `Services/CinemaDbContext.cs` (jeśli testuje lokalnie)

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    var connectionString = "Host=localhost;Database=cinema_db;Username=cinema_user;Password=CinemaPass123!;Port=5432";
    optionsBuilder.UseNpgsql(connectionString, ...);
}
```

**LUB** użyj zmiennej środowiskowej (zalecane):

```powershell
# Ustaw zmienną środowiskową (Windows)
[Environment]::SetEnvironmentVariable(
    "DATABASE_URL", 
    "Host=localhost;Database=cinema_db;Username=cinema_user;Password=CinemaPass123!;Port=5432",
    "User"
)

# Lub tymczasowo w PowerShell:
$env:DATABASE_URL = "Host=localhost;Database=cinema_db;Username=cinema_user;Password=CinemaPass123!;Port=5432"
```

### 2. Uruchom migracje Entity Framework

```powershell
cd c:\Users\Szymon Tkaczyk\Documents\CA_v29\CA

# Utwórz nową migrację (jeśli zmienił się model)
dotnet ef migrations add InitialPostgres

# Zastosuj migrację do bazy
dotnet ef database update
```

### 3. Weryfikacja bazy

```powershell
# Zaloguj się do bazy
psql -U cinema_user -d cinema_db

# Wyświetl wszystkie tablice
\dt

# Wyświetl schemat konkretnej tablicy
\d reservations

# Wyjdź
\q
```

---



## WIELOWĄTKOWY DOSTĘP (2+ instancject)



1. **Transaction Support** - każda operacja Save pracuje z transakcją:
   ```csharp
   using var transaction = await context.Database.BeginTransactionAsync();
   try
   {
       await context.SaveChangesAsync();
       await transaction.CommitAsync();
   }
   catch
   {
       await transaction.RollbackAsync();
   }
   ```

2. **Concurrency Control** - [Timestamp] attribute na Reservation i Ticket:
   ```csharp
   [Timestamp]
   public byte[]? RowVersion { get; set; }
   ```
   - PostgreSQL automatycznie aktualizuje przy zmianie
   - DbUpdateConcurrencyException jeśli konflikt

3. **Async Operations** - wszystkie SaveChanges są async:
   ```csharp
   await context.SaveChangesAsync();  // Instead of context.SaveChanges()
   ```

4. **Connection Pooling** - Npgsql zarządza pulą połączeń:
   ```csharp
   optionsBuilder.UseNpgsql(
       connectionString,
       options => options
           .EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), ...) // Auto-retry
           .CommandTimeout(30)
   );
   ```

---

Aby przetestować czy 2 instancje mogą pracować razem:

### Instancja 1 (Kasjer A)
```powershell
cd C:\path\to\app
dotnet run --project CinemaSystem.Desktop.csproj
```

### Instancja 2 (Kasjer B) - NOWE OKNO POWERSHELL
```powershell
cd C:\path\to\app
dotnet run --project CinemaSystem.Desktop.csproj
```

### Test konkurencji:

1. **Kasjer A:** Otwiera rezerwację #1
2. **Kasjer B:** Otwiera tę samą rezerwację #1
3. **Kasjer A:** Zmienia email klienta, klika ZAPISZ
4. **Kasjer B:** Zmienia email klienta, klika ZAPISZ
   -  Otrzyma błąd: "Konflikt dostępu: inny użytkownik zmienił dane"
   -  Prawidłowe zachowanie (concurrency control działa!)

---

## TROUBLESHOOTING

### Problem: "psql: command not found"

**Rozwiązanie:** Dodaj PostgreSQL do PATH:

```powershell
# Edytuj zmienne środowiskowe
$env:PATH += ";C:\Program Files\PostgreSQL\15\bin"

# Lub dodaj permanentnie (System Properties → Environment Variables)
```

### Problem: "Connection refused on 127.0.0.1:5432"

**Rozwiązanie:** Sprawdź czy PostgreSQL jest uruchomiony:

```powershell
# Windows Services
Get-Service | grep postgres

# Albo
services.msc  # I szukaj PostgreSQL-x64-15

# Jeśli zatrzymany, uruchom:
Start-Service PostgreSQL-x64-15
```

### Problem: "Password authentication failed for user \"cinema_user\""

**Rozwiązanie:** Resetuj hasło:

```powershell
psql -U postgres

postgres=# ALTER USER cinema_user WITH PASSWORD 'NewPassword123!';
postgres=# \q
```

### Problem: "FATAL: database \"cinema_db\" does not exist"

**Rozwiązanie:**

```powershell
psql -U postgres

postgres=# CREATE DATABASE cinema_db ENCODING 'UTF8';
postgres=# \q
```

---

## DOKUMENTACJA

- **PostgreSQL Docs:** https://www.postgresql.org/docs/15/
- **Entity Framework Core PostgreSQL:** https://www.npgsql.org/efcore/
- **Railway.app:** https://docs.railway.app/
- **Render.com:** https://render.com/docs/databases

---

# [App starts successfully]
```

🎉 **Gotowe! Cinema System teraz pracuje na PostgreSQL z obsługą wielowątkowego dostępu!**
