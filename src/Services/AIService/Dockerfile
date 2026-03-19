# Multi-stage build for Python AI Service
FROM python:3.11-slim AS builder

WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    gcc \
    g++ \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements first for better caching
COPY requirements.txt .
COPY requirements-dev.txt .

# Upgrade pip first with verbose logging
RUN --mount=type=cache,target=/root/.cache/pip \
    pip install --no-cache-dir --upgrade pip --verbose && \
    echo "=== Pip upgraded successfully ==="

# Install base dependencies first (without torch-heavy packages)
RUN --mount=type=cache,target=/root/.cache/pip \
    pip install --no-cache-dir \
        --timeout=300 \
        --retries=5 \
        --verbose \
        --progress-bar=on \
        fastapi==0.115.0 \
        uvicorn[standard]==0.32.0 \
        python-multipart==0.0.18 \
        grpcio==1.76.0 \
        grpcio-tools==1.76.0 \
        grpcio-status==1.76.0 \
        protobuf==6.31.1 \
        langchain>=0.3.4 \
        langchain-openai==0.2.0 \
        langchain-community>=0.3.3 \
        openai>=1.54.0 \
        httpx>=0.27.0 \
        qdrant-client==1.11.0 \
        neo4j>=5.25.0 \
        langchain-neo4j==0.1.0 \
        cohere==4.47 \
        networkx==3.2.1 \
        pydantic>=2.10.0 \
        pydantic-settings>=2.6.0 \
        python-dotenv==1.0.1 \
        structlog==24.2.0 \
        aiofiles==24.1.0 \
        aiosqlite==0.22.0 \
        psycopg[binary]==3.3.2 \
        psycopg-pool==3.3.0 && \
    echo "=== Base dependencies installed successfully ==="

# Install torch CPU-only version to avoid CUDA dependencies (saves ~600MB)
# If GPU is needed, remove --index-url and install CUDA version
RUN --mount=type=cache,target=/root/.cache/pip \
    echo "=== Starting torch CPU-only installation (this may take several minutes) ===" && \
    pip install --no-cache-dir \
        --timeout=600 \
        --retries=10 \
        --verbose \
        --progress-bar=on \
        --index-url https://download.pytorch.org/whl/cpu \
        torch==2.9.1 && \
    echo "=== Torch CPU-only installed successfully ===" && \
    echo "=== Starting sentence-transformers installation ===" && \
    pip install --no-cache-dir \
        --timeout=300 \
        --retries=5 \
        --verbose \
        --progress-bar=on \
        sentence-transformers==3.0.0 && \
    echo "=== All dependencies installed successfully ==="

RUN --mount=type=cache,target=/root/.cache/pip \
    pip install --no-cache-dir \
        --timeout=300 \
        --retries=5 \
        --verbose \
        --progress-bar=on \
        -r requirements.txt && \
    echo "=== requirements.txt installed successfully ==="

# Runtime stage
FROM python:3.11-slim

WORKDIR /app

# Install runtime dependencies
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy Python dependencies from builder
COPY --from=builder /usr/local/lib/python3.11/site-packages /usr/local/lib/python3.11/site-packages
COPY --from=builder /usr/local/bin /usr/local/bin

# Copy application code
COPY app ./app
COPY requirements.txt ./
# COPY proto ./proto

# Create non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Expose ports
EXPOSE 8000

# Health check
# Use PORT environment variable if set, otherwise default to 8000
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD sh -c "curl -f http://localhost:${PORT:-8000}/health || exit 1"

# Run application
# Use PORT environment variable if set, otherwise default to 8000
CMD sh -c "uvicorn app.main:app --host 0.0.0.0 --port ${PORT:-8000}"

