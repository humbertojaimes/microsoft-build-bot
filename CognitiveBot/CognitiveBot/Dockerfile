FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 3978
EXPOSE 3010

ARG DataSource
ENV DataSource=${DataSource} 
ARG DbUser
ENV DbUser=${DbUser} 
ARG Password
ENV Password=${Password} 
ARG LanguageEndPoint
ENV LanguageEndPoint=${LanguageEndPoint} 
ARG TextEndPoint
ENV TextEndPoint=${TextEndPoint} 
ARG LuisEndPoint
ENV LuisEndPoint=${LuisEndPoint} 
ARG LuisAppId
ENV LuisAppId=${LuisAppId} 
ARG DirectLine
ENV DirectLine=${DirectLine} 

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src

COPY libraries .
COPY CognitiveBot/CognitiveBot.csproj ./
RUN dotnet restore ./CognitiveBot.csproj


COPY . .
RUN dotnet build CognitiveBot/CognitiveBot.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish CognitiveBot/CognitiveBot.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CognitiveBot.dll"]

