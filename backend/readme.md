Install the EF Core CLI tools:
dotnet tool install --global dotnet-ef

dotnet ef migrations add InitialCreate
dotnet ef migrations remove
dotnet ef database update


dotnet ef database update -v

dotnet ef database update InitialMigration -v
dotnet ef migrations remove

dotnet ef migrations script -o ./script.sql
dotnet ef migrations script -o ./script.sql -i

https://learn.microsoft.com/en-us/ef/core/cli/dotnet
https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli