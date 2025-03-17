Install the EF Core CLI tools:
dotnet tool install --global dotnet-ef

INSTRUCTIONS FOR CHANGING DATABASE:
1. add changes in c#
2. run this: dotnet ef migrations add [INSERT NAME HERE]
3. Restart backend container
4. DO NOT DO ANY MANUAL CHANGES TO THE DATABASE!!!!!!


dotnet ef migrations remove
dotnet ef database update


dotnet ef database update -v

dotnet ef database update InitialMigration -v
dotnet ef migrations remove

dotnet ef migrations script -o ./script.sql
dotnet ef migrations script -o ./script.sql -i

https://learn.microsoft.com/en-us/ef/core/cli/dotnet
https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli


