# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copia tudo e restaura as dependências
COPY . ./
RUN dotnet restore

# Compila o projeto
RUN dotnet publish -c Release -o out

# Estágio de Runtime (Execução)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expõe a porta que o Render usa
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "SSSite.dll"]