#!/bin/sh

echo "Starting Piper server..."

python3 -m piper.http_server \
  -m /app/Piper/voices/en_US-ryan-medium.onnx \
  --host 127.0.0.1 \
  --port 5000 &

echo "Starting Dwuma API..."

exec dotnet Dwuma.dll