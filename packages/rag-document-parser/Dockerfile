FROM python:3.12-slim

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PIP_NO_CACHE_DIR=1 \
    HOST=0.0.0.0 \
    PORT=8000 \
    RELOAD=false \
    TESSERACT_PATH=/usr/bin/tesseract \
    LIBREOFFICE_PATH=/usr/bin/soffice

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libreoffice \
        tesseract-ocr \
        tesseract-ocr-eng \
        tesseract-ocr-chi-sim \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY pyproject.toml README.md requirements.txt /app/
COPY src /app/src

RUN pip install --upgrade pip \
    && pip install .

EXPOSE 8000

CMD ["python", "-m", "uvicorn", "rag_document_parser.api.app:app", "--host", "0.0.0.0", "--port", "8000"]
