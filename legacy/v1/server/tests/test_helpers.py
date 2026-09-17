"""Unit tests for pure helper functions in main.py."""
import pytest


def test_allowed_file(app_db):
    assert app_db.allowed_file("photo.png") is True
    assert app_db.allowed_file("photo.JPG") is True
    assert app_db.allowed_file("scan.jpeg") is True
    assert app_db.allowed_file("archive.zip") is False
    assert app_db.allowed_file("noextension") is False


def test_dict_from_row_none(app_db):
    assert app_db.dict_from_row(None) is None


def test_dict_from_row_maps_columns(db_conn, app_db):
    row = db_conn.execute("SELECT username FROM users WHERE username='admin'").fetchone()
    d = app_db.dict_from_row(row)
    assert isinstance(d, dict)
    assert d["username"] == "admin"


def test_generate_next_patient_id_starts_at_one(app_db):
    # Fresh DB has no patients -> first id is zero-padded 000001
    assert app_db.generate_next_patient_id() == "000001"


def test_generate_next_patient_id_increments(db_conn, app_db):
    db_conn.execute(
        "INSERT INTO patients (id, patient_name) VALUES (?, ?)", ("000042", "Zhang")
    )
    db_conn.commit()
    assert app_db.generate_next_patient_id() == "000043"


def test_generate_next_patient_id_limit(db_conn, app_db):
    db_conn.execute(
        "INSERT INTO patients (id, patient_name) VALUES (?, ?)", ("999999", "Max")
    )
    db_conn.commit()
    with pytest.raises(ValueError):
        app_db.generate_next_patient_id()
