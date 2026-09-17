"""
Pytest fixtures for EZ-Dose server tests.

Isolation strategy:
- main.py runs init_db() at import time and uses RELATIVE paths
  ('data/ezdose.db', 'static/images', 'data/ezdose.log'). To avoid touching
  the real repo database/logs, we chdir into a temp directory BEFORE importing
  main, so all import-time side effects land in throwaway dirs.
- get_db_connection() reads the module-global main.DATABASE_FILE at call time,
  so each test points it at its own temp SQLite file and calls init_db().
"""
import os
import shutil
import tempfile
import importlib
import sys

import pytest

# ---- Import-time isolation: chdir into a temp dir before importing main ----
_IMPORT_CWD = tempfile.mkdtemp(prefix="ezdose-import-")
_ORIG_CWD = os.getcwd()
# main.py resolves templates via its own file location, so chdir is safe for
# Jinja; only relative data/static paths are affected, which is what we want.
_REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, _REPO_ROOT)
os.chdir(_IMPORT_CWD)
import main  # noqa: E402  (import after chdir on purpose)
os.chdir(_ORIG_CWD)


def _cleanup_import_dir():
    shutil.rmtree(_IMPORT_CWD, ignore_errors=True)


@pytest.fixture
def app_db(tmp_path, monkeypatch):
    """Point main at a fresh temp SQLite DB and (re)create the schema."""
    db_file = tmp_path / "test_ezdose.db"
    monkeypatch.setattr(main, "DATABASE_FILE", str(db_file))
    main.app.config.update(TESTING=True, WTF_CSRF_ENABLED=False)
    main.app.secret_key = "test-secret-key"
    main.init_db()  # creates tables + default admin (admin/admin123)
    return main


@pytest.fixture
def client(app_db):
    """Anonymous test client backed by an isolated DB."""
    return app_db.app.test_client()


@pytest.fixture
def auth_client(app_db):
    """Test client logged in as the default full-permission admin user."""
    c = app_db.app.test_client()
    c.post("/login", data={"username": "admin", "password": "admin123"})
    return c


@pytest.fixture
def db_conn(app_db):
    """Direct DB connection for arranging/asserting rows in tests."""
    conn = app_db.get_db_connection()
    yield conn
    conn.close()


def pytest_sessionfinish(session, exitstatus):
    _cleanup_import_dir()
