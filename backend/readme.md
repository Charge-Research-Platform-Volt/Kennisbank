INSTRUCTIONS FOR CHANGING DATABASE:
1. add changes in c#
2. Restart backend container
3. DO NOT DO ANY MANUAL CHANGES TO THE DATABASE!!!!!!
4. In case of manual changes do not panic, but follow the following steps:
    a. Convert your manual changes to C#
    b. Shut down backend
    c. Restart database
    d. Start backend

IGNORE THIS (CAN BE REMOVED???):
dotnet ef migrations remove
dotnet ef database update


dotnet ef database update -v

dotnet ef database update InitialMigration -v
dotnet ef migrations remove

dotnet ef migrations script -o ./script.sql
dotnet ef migrations script -o ./script.sql -i

https://learn.microsoft.com/en-us/ef/core/cli/dotnet
https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli

This program has been developed by students from the bachelor Computer Science at Utrecht
University within the Software Project course.
© Copyright Utrecht University (Department of Information and Computing Sciences)