from fastapi.testclient import TestClient

from app.main import create_app


def test_liveness_without_database():
    with TestClient(create_app()) as client:
        response = client.get("/health/live")
        assert response.status_code == 200
        assert response.json() == {"status": "ok", "service": "mdis-api"}
        assert client.get("/api/patients").status_code == 404
