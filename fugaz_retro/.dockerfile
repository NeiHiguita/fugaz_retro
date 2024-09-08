# Usa la imagen base de .NET SDK para compilar y publicar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copia los archivos del proyecto y restaura las dependencias
COPY *.csproj ./
RUN dotnet restore

# Copia el resto de los archivos y compila la aplicación
COPY . ./
RUN dotnet publish -c Release -o out

# Usa la imagen base de .NET Runtime para ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Expone el puerto en el que la aplicación escuchará
EXPOSE 80

# Define el comando para ejecutar la aplicación
ENTRYPOINT ["dotnet", "fugaz_retro.dll"]
