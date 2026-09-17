"""Integration tests for /packer/* device-sync API endpoints."""
import json


def _add_patient(conn, pid="000001", name="Li"):
    conn.execute(
        "INSERT INTO patients (id, patient_name, bed_number) VALUES (?, ?, ?)",
        (pid, name, "12"),
    )
    conn.commit()


def _add_prescription(conn, patient_id="000001", **kw):
    fields = {
        "medicine_name": "Aspirin",
        "morning_dosage": 1, "noon_dosage": 0, "evening_dosage": 1,
        "meal_timing": "after_meal", "start_date": "2026-07-01",
        "duration_days": 7, "is_active": 1,
        "motor_speed": 0.5, "servo_angle": 0.7, "image_resource_id": "img_a.png",
    }
    fields.update(kw)
    cur = conn.execute(
        """INSERT INTO prescriptions
           (patient_id, medicine_name, morning_dosage, noon_dosage, evening_dosage,
            meal_timing, start_date, duration_days, is_active, motor_speed, servo_angle, image_resource_id)
           VALUES (?,?,?,?,?,?,?,?,?,?,?,?)""",
        (patient_id, fields["medicine_name"], fields["morning_dosage"],
         fields["noon_dosage"], fields["evening_dosage"], fields["meal_timing"],
         fields["start_date"], fields["duration_days"], fields["is_active"],
         fields["motor_speed"], fields["servo_angle"], fields["image_resource_id"]),
    )
    conn.commit()
    return cur.lastrowid


def test_index_status(client):
    resp = client.get("/")
    assert resp.status_code == 200
    body = resp.get_json()
    assert body["database"] == "SQLite"
    assert any("/packer/patients" in e for e in body["available_endpoints"])


def test_get_patients_empty(client):
    resp = client.get("/packer/patients")
    assert resp.status_code == 200
    body = resp.get_json()
    assert body["success"] is True
    assert body["count"] == 0
    assert body["data"] == []


def test_get_patients_returns_rows(client, db_conn):
    _add_patient(db_conn, "000001", "Li")
    resp = client.get("/packer/patients")
    body = resp.get_json()
    assert body["count"] == 1
    assert body["data"][0]["patient_name"] == "Li"


def test_get_pill_boxes_returns_active_rfid_bindings(client, db_conn):
    _add_patient(db_conn, "000001", "Li")
    db_conn.execute(
        "INSERT INTO pill_boxes (patient_id, rfid_uid) VALUES (?, ?)",
        ("000001", "5303859E740001"),
    )
    db_conn.commit()

    resp = client.get("/packer/pill-boxes")
    assert resp.status_code == 200
    body = resp.get_json()
    assert body["success"] is True
    assert body["count"] == 1
    assert body["data"][0]["rfid_uid"] == "5303859E740001"
    assert body["data"][0]["patient_id"] == "000001"


def test_patient_form_binds_and_normalizes_multiple_rfid_uids(auth_client, db_conn):
    resp = auth_client.post(
        "/admin/patients/add",
        data={
            "patient_name": "Wang",
            "bed_number": "7",
            "rfid_uids": "uid:5303859e740001\nA1B2C3D4",
        },
    )
    assert resp.status_code == 302

    rows = db_conn.execute(
        "SELECT patient_id, rfid_uid FROM pill_boxes ORDER BY id"
    ).fetchall()
    assert [(row["patient_id"], row["rfid_uid"]) for row in rows] == [
        ("000001", "5303859E740001"),
        ("000001", "A1B2C3D4"),
    ]


def test_rfid_uid_cannot_be_bound_to_two_patients(auth_client, db_conn):
    _add_patient(db_conn, "000001", "Li")
    db_conn.execute(
        "INSERT INTO pill_boxes (patient_id, rfid_uid) VALUES (?, ?)",
        ("000001", "5303859E740001"),
    )
    db_conn.commit()

    resp = auth_client.post(
        "/admin/patients/add",
        data={
            "patient_name": "Wang",
            "bed_number": "8",
            "rfid_uids": "5303859E740001",
        },
    )
    assert resp.status_code == 200
    assert "已绑定到患者 000001" in resp.get_data(as_text=True)
    assert db_conn.execute(
        "SELECT COUNT(*) FROM patients WHERE patient_name = 'Wang'"
    ).fetchone()[0] == 0


def test_upload_patients_camelcase_alias(client):
    resp = client.post(
        "/packer/patients/upload",
        data=json.dumps({"patients": [{"patientName": "Wang", "patientBedNumber": "7"}]}),
        content_type="application/json",
    )
    assert resp.status_code == 200
    body = resp.get_json()
    assert body["count"] == 1
    # ID generated as 6-digit zero-padded
    listed = client.get("/packer/patients").get_json()["data"]
    assert listed[0]["id"] == "000001"
    assert listed[0]["patient_name"] == "Wang"


def test_upload_patients_bad_format(client):
    resp = client.post(
        "/packer/patients/upload",
        data=json.dumps({"wrong": []}),
        content_type="application/json",
    )
    assert resp.status_code == 400
    assert resp.get_json()["success"] is False


def test_get_prescriptions_only_active(client, db_conn):
    _add_patient(db_conn)
    _add_prescription(db_conn, is_active=1, medicine_name="Active")
    _add_prescription(db_conn, is_active=0, medicine_name="Inactive")
    body = client.get("/packer/prescriptions").get_json()
    names = [r["medicine_name"] for r in body["data"]]
    assert "Active" in names
    assert "Inactive" not in names
    # JOIN brings patient_name through
    assert body["data"][0]["patient_name"] == "Li"


def test_upload_prescription_insert_new(client, db_conn):
    _add_patient(db_conn)
    resp = client.post(
        "/packer/prescriptions/upload",
        data=json.dumps({"prescriptions": [
            {"patient_id": "000001", "medicine_name": "NewMed",
             "morning_dosage": 2, "duration_days": 5}
        ]}),
        content_type="application/json",
    )
    assert resp.status_code == 200
    row = db_conn.execute(
        "SELECT * FROM prescriptions WHERE medicine_name='NewMed'"
    ).fetchone()
    assert row is not None
    assert row["morning_dosage"] == 2


def test_coalesce_preserves_dispenser_settings(client, db_conn):
    """Device sync must NOT overwrite motor_speed and servo_angle when it sends 0/null."""
    _add_patient(db_conn)
    rx_id = _add_prescription(db_conn, motor_speed=0.5, servo_angle=0.7, image_resource_id="cal.png")

    # Device re-syncs the same prescription but sends 0 / empty for settings fields
    resp = client.post(
        "/packer/prescriptions/upload",
        data=json.dumps({"prescriptions": [
            {"id": rx_id, "patient_id": "000001", "medicine_name": "Aspirin",
             "motor_speed": 0, "servo_angle": 0, "image_resource_id": ""}
        ]}),
        content_type="application/json",
    )
    assert resp.status_code == 200
    row = db_conn.execute(
        "SELECT motor_speed, servo_angle, image_resource_id FROM prescriptions WHERE id=?", (rx_id,)
    ).fetchone()
    assert row["motor_speed"] == 0.5            # preserved, not zeroed
    assert row["servo_angle"] == 0.7            # preserved, not zeroed
    assert row["image_resource_id"] == "cal.png"  # preserved, not blanked


def test_upload_prescription_updates_new_dispenser_settings(client, db_conn):
    """When device DOES send real calibrated values, they must be written."""
    _add_patient(db_conn)
    rx_id = _add_prescription(db_conn, motor_speed=0.5, servo_angle=0.7)
    client.post(
        "/packer/prescriptions/upload",
        data=json.dumps({"prescriptions": [
            {"id": rx_id, "patient_id": "000001", "medicine_name": "Aspirin",
             "motor_speed": 0.8, "servo_angle": 0.3, "image_resource_id": "new.png"}
        ]}),
        content_type="application/json",
    )
    row = db_conn.execute(
        "SELECT motor_speed, servo_angle, image_resource_id FROM prescriptions WHERE id=?", (rx_id,)
    ).fetchone()
    assert row["motor_speed"] == 0.8
    assert row["servo_angle"] == 0.3
    assert row["image_resource_id"] == "new.png"


def test_dispense_records_log(client, db_conn):
    _add_patient(db_conn)
    rx_id = _add_prescription(db_conn)
    resp = client.post(
        "/packer/dispense",
        data=json.dumps({
            "dispense_date": "2026-07-21", "patient_id": "000001",
            "prescription_id": rx_id, "medicine_name": "Aspirin",
            "dosage": 1, "time_period": "morning",
        }),
        content_type="application/json",
    )
    assert resp.status_code == 200
    assert resp.get_json()["success"] is True
    logs = client.get("/packer/dispense_logs").get_json()
    assert logs["count"] == 1
    assert logs["data"][0]["time_period"] == "morning"


def test_dispense_missing_field(client):
    resp = client.post(
        "/packer/dispense",
        data=json.dumps({"patient_id": "000001"}),
        content_type="application/json",
    )
    assert resp.status_code == 400
    assert "Missing required field" in resp.get_json()["message"]


def test_calibration_default_and_update(client):
    body = client.get("/packer/settings/calibration").get_json()
    assert body["success"] is True
    assert body["data"]["reference_pill_diameter_mm"] == 9.0

    client.post(
        "/packer/settings/calibration",
        data=json.dumps({"reference_pill_diameter_mm": 11.5}),
        content_type="application/json",
    )
    updated = client.get("/packer/settings/calibration").get_json()
    assert updated["data"]["reference_pill_diameter_mm"] == 11.5
