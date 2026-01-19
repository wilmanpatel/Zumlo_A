
# Backend — Zumlo Voice Conversation Gateway (.NET 8)

Run:
```bash
cd backend
Dotnet build Zumlo.Gateway.sln
cd Zumlo.Gateway.Api
DOTNET_URLS=http://localhost:5099 dotnet run
```

Endpoints:
- WebSocket: `ws://localhost:5099/ws`
- GET `/api/sessions/{id}/summary`

Auth: send `Authorization: Bearer dev` (any non-empty token is accepted).
