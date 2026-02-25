# GitHub Copilot Instructions

## Server Connection Workflow (for server-side change requests)

When a prompt asks to change something **on the server** refer to .env file for details of ec2 server.

When a prompt explicitly asks to **connect to the server**, run an SSH command using the credentials from `.env` (for example: `SSH_PEM_PATH`, `SERVER_USER`, `SERVER_HOST`, `SERVER_PORT`) as the first step.


## Execution Style

When performing server tasks, execute operations **one step at a time** as separate, granular commands. Break multi-step processes into individual operations.

### ❌ Bad Example (Do NOT do this)
```bash
ssh root@203.0.113.10 'bash -s' <<'SSH'
set -e
cd /srv/login/LoginAPI
git pull
dotnet publish -c Release
sudo systemctl restart loginapi
curl -fsS http://localhost:8080/health
SSH
```

### ✅ Good Example (Do this instead)
```bash
# Step 1: Read server values from env file
source .env.server

# Step 2: Verify API is reachable before change
ssh -i "$SSH_PEM_PATH" "$SERVER_USER@$SERVER_HOST" -p "$SERVER_PORT" "curl -fsS http://localhost:8080/health"

# Step 3: Check current service state
ssh -i "$SSH_PEM_PATH" "$SERVER_USER@$SERVER_HOST" -p "$SERVER_PORT" "sudo systemctl status loginapi --no-pager"

# Step 4: Build/publish LoginAPI on server
ssh -i "$SSH_PEM_PATH" "$SERVER_USER@$SERVER_HOST" -p "$SERVER_PORT" "cd /srv/login/LoginAPI && dotnet publish -c Release"

# Step 5: Restart LoginAPI service
ssh -i "$SSH_PEM_PATH" "$SERVER_USER@$SERVER_HOST" -p "$SERVER_PORT" "sudo systemctl restart loginapi"

# Step 6: Validate health endpoint after restart
ssh -i "$SSH_PEM_PATH" "$SERVER_USER@$SERVER_HOST" -p "$SERVER_PORT" "curl -fsS http://localhost:8080/health"
```


