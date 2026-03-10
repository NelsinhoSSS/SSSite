# Estágio de Build - Usando SDK 10.0
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build-env
WORKDIR /app

# Copia tudo e restaura as dependências
COPY . ./
RUN dotnet restore

# Compila o projeto
RUN dotnet publish -c Release -o out

# Estágio de Runtime (Execução) - Usando ASP.NET 10.0
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build-env /app/out .

# Expõe a porta que o Render usa
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "SSSite.dll"]