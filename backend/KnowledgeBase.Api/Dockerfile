FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore KnowledgeBase.Api/KnowledgeBase.Api.csproj
RUN dotnet publish KnowledgeBase.Api/KnowledgeBase.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# 老式 .doc(Word 97-2003) 通过 LibreOffice headless 转换为文本。
# 使用 --no-install-recommends 控制镜像体积，DOCX/XLS/XLSX/PDF 不依赖 LibreOffice。
RUN apt-get update \
    && apt-get install -y --no-install-recommends libreoffice-writer \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data /app/storage
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "KnowledgeBase.Api.dll"]
