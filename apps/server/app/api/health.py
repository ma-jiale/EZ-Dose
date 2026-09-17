from fastapi import APIRouter

router = APIRouter(prefix="/health", tags=["health"])


@router.get("/live")
def liveness() -> dict[str, str]:
    """Process liveness only; does not assert database or device readiness."""
    return {"status": "ok", "service": "mdis-api"}
