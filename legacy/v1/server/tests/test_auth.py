"""Tests for authentication and authorization behaviour."""


def test_login_success_redirects_to_admin(client):
    resp = client.post(
        "/login", data={"username": "admin", "password": "admin123"}
    )
    # Successful login redirects to the admin dashboard
    assert resp.status_code == 302
    assert "/admin" in resp.headers["Location"]


def test_login_wrong_password(client):
    resp = client.post(
        "/login", data={"username": "admin", "password": "wrong"}
    )
    # Stays on the login page (200) and does not redirect
    assert resp.status_code == 200
    assert "user_id" not in _session_keys(client)


def test_login_required_redirects_anonymous(client):
    resp = client.get("/admin")
    assert resp.status_code == 302
    assert "/login" in resp.headers["Location"]


def test_authenticated_can_reach_admin(auth_client):
    resp = auth_client.get("/admin")
    assert resp.status_code == 200


def test_permission_denied_returns_403(app_db, db_conn):
    """A user lacking can_edit_users should get 403 on user management."""
    from werkzeug.security import generate_password_hash
    db_conn.execute(
        """INSERT INTO users (username, password_hash, can_edit_users,
             can_edit_patients, can_edit_prescriptions, can_view_logs)
           VALUES (?, ?, 0, 0, 0, 0)""",
        ("nurse", generate_password_hash("nurse123")),
    )
    db_conn.commit()
    c = app_db.app.test_client()
    c.post("/login", data={"username": "nurse", "password": "nurse123"})
    resp = c.get("/admin/users/add")
    assert resp.status_code == 403


def _session_keys(client):
    with client.session_transaction() as sess:
        return list(sess.keys())
