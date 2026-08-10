# ----------------------------
# BUILD
# ----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["Dwuma.csproj", "./"]
RUN dotnet restore "Dwuma.csproj"

COPY . .

RUN dotnet publish "Dwuma.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ----------------------------
# RUNTIME
# ----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Install Python, pip and curl
RUN apt-get update \
    && apt-get install -y python3 python3-pip curl \
    && pip3 install --break-system-packages piper-tts \
    && rm -rf /var/lib/apt/lists/*

# Copy published .NET application
COPY --from=build /app/publish .

# Create Piper voice directory
RUN mkdir -p /app/Piper/voices

# Download Ryan voice directly during build
RUN curl -L \
    "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium/en_US-ryan-medium.onnx?download=true" \
    -o /app/Piper/voices/en_US-ryan-medium.onnx

RUN curl -L \
    "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/ryan/medium/en_US-ryan-medium.onnx.json?download=true" \
    -o /app/Piper/voices/en_US-ryan-medium.onnx.json

# Verify model exists during build
RUN ls -lh /app/Piper/voices

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["dotnet", "Dwuma.dll"]