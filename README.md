# Your Order We Deliver – Delivery Management System

SWE30003 Assignment 3, Group 7. C# console application (.NET 8).

## Run

Requires the .NET 8 SDK (or runtime for the prebuilt version).

- **From source:** `dotnet run --project DeliveryManagement` (add `-- --fresh` to start with no saved requests)
- **Prebuilt:** `dotnet Executable/DeliveryManagement.dll` (add `--fresh` as above)
- **Visual Studio:** open `DeliveryManagement.sln` and press F5.

Data is saved to `dms-data.json` next to the program.

## Test accounts

| Role | Email | Password |
|---|---|---|
| Customer | alice@example.com | alice123 |
| Customer | ben@example.com | ben123 |
| Office staff | olivia@ywd.com.au | office123 |
| Driver | dan@ywd.com.au | driver123 |
| Driver | priya@ywd.com.au | driver123 |
| Driver | sam@ywd.com.au | driver123 |

Card numbers ending in `0000` are always declined (payments are simulated).

## Structure

- `Program.cs` – entry point
- `DeliveryManagementSystem.cs` – facade
- `Models/` – domain classes
- `Services/` – business logic and input checks
- `Data/` – JSON repository
- `UI/` – console screens

Coding standard: Microsoft C# Coding Conventions and .NET naming guidelines, enforced by `.editorconfig`.
