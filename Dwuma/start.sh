#!/bin/sh

echo "Starting Piper server..."

python3 -m piper.http_server \
  -m /app/Piper/voices/en_US-ryan-medium.onnx \
  --host 127.0.0.1 \
  --port 5000 &

echo "Waiting for Piper..."

until curl -sf http://127.0.0.1:5000/info > /dev/null; do
  sleep 1
done

echo "Piper is ready."

echo "Starting Dwuma API..."

exec dotnet Dwuma.dll