FROM python:3.12-slim
WORKDIR /app
COPY apps/server/pyproject.toml ./
COPY apps/server/app ./app
RUN pip install --no-cache-dir . && useradd --create-home mdis
USER mdis
EXPOSE 8000
CMD ["python", "-m", "uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
