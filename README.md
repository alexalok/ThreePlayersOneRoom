# How to run

1. Navigate to the solution root folder

2. Create an empty database in the root folder

  `dotnet ef database update -p ThreePlayersOneRoom/ThreePlayersOneRoom.csproj --connection "Data Source=$(pwd)/db.sqlite"`

3. Build an image

  `docker build -f ThreePlayersOneRoom/Dockerfile -t 3p1r .`

4. Run container

  `docker run -it -p 5281:80 -v "$(pwd):/app/data/" 3p1r DbName="/app/data/db.sqlite"`



