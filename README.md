[README.md](https://github.com/user-attachments/files/32772899/README.md)
# GameRash

A game storefront web app built with ASP.NET MVC — browse games, manage a
wishlist and library, leave reviews, and simulate purchases/payments, with
separate admin functionality.

## Features

- **Auth** — user registration and login with hashed passwords, session-based auth
- **Games** — browse games and view game details
- **Library** — track owned games per user
- **Wishlist** — save games to a wishlist
- **Reviews** — leave and view reviews on games
- **Purchases & Payments** — simulated purchase and payment flow
- **Admin** — admin-side management endpoints

## Tech stack

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core 9 with SQL Server
- Swagger / Swashbuckle for API exploration

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (installed automatically with Visual Studio's ASP.NET
  workload, or available as a standalone [SQL Server Express
  install](https://www.microsoft.com/en-us/sql-server/sql-server-downloads))

## Setup

1. Clone the repo and restore dependencies:
   ```
   dotnet restore
   ```
2. Create the database (the connection string in `appsettings.json` points to
   LocalDB by default — edit it if you're using a different SQL Server
   instance):
   ```
   dotnet ef database update
   ```
3. Run the app:
   ```
   dotnet run
   ```
4. Open the URL shown in the terminal (usually `https://localhost:5001` or
   similar).

## Project structure

```
Controllers/   MVC controllers (Auth, Game, Library, Payment, etc.)
Models/        Entity/domain models
Data/          EF Core DbContext
Migrations/    EF Core migrations
Views/         Razor views
wwwroot/       Static assets (CSS, JS, images)
```

## Notes

Built as a learning project exploring full-stack web development with
ASP.NET MVC and Entity Framework. Seed data uses placeholder credentials for
demonstration only.

## License

MIT — see [LICENSE.txt](LICENSE.txt).
