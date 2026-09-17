# Mdis Agent Rules

1. Active V2 development lives in apps/.
2. legacy/ is read-only unless explicitly requested.
3. hardware/ is active engineering code.
4. Read docs/ before making architectural changes.
5. Machine control happens locally on the client.
6. Never blindly retry physical dispensing commands.
7. Server-side tenant access must be derived from authenticated membership.
8. Do not trust a client-provided nursing-home ID.
9. Business state and sync state are separate.
10. Do not infer current behavior from legacy/v0.
11. Use legacy/v1 only to verify validated V1 behavior.
12. Do not commit patient data, databases, secrets, SDKs or build output.
