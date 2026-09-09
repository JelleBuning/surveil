#!/usr/bin/env python3
"""
PreToolUse hook: Blocks read/write access to sensitive files.

Hook event: PreToolUse (matcher: Read|Write|Edit|MultiEdit)
Exit code 2 = block the tool call with an error message.

Protects against Claude accidentally reading or writing:
  - Credential files (.env, .pem, private keys, tokens)
  - Sensitive directories (~/.ssh, ~/.aws, ~/.gnupg)
  - User-defined blocked paths via CLAUDE_BLOCKED_PATHS env var

Works for everyone — developers, founders, designers, students.

Customize:
  - Add filenames to BLOCKED_NAMES
  - Add extensions to BLOCKED_EXTS
  - Add directory patterns to BLOCKED_DIRS
  - Set CLAUDE_BLOCKED_PATHS env var for personal folders:
    export CLAUDE_BLOCKED_PATHS="/Users/me/Financials:/Users/me/Clients"
"""
import json, sys, os
from pathlib import Path

# --- Blocked filenames (exact match) ---
BLOCKED_NAMES = {
    # Environment files
    '.env', '.env.local', '.env.production', '.env.staging', '.env.development',
    '.env.test', '.env.backup', '.env.bak', '.env.old',
    # Credential files
    'secrets.json', '.secrets', 'credentials.json', 'credentials.yml',
    'credentials.yaml', 'secrets.yml', 'secrets.yaml',
    # SSH keys
    'id_rsa', 'id_ed25519', 'id_ecdsa', 'id_dsa',
    'id_rsa.pub', 'id_ed25519.pub',  # pub keys can leak username/host info
    'known_hosts', 'authorized_keys',
    # Cloud credentials
    'service-account.json', 'gcp-credentials.json',
    'aws-credentials',  # files inside ~/.aws/ are covered by BLOCKED_DIRS
    # Package manager tokens
    '.npmrc', '.pypirc', '.yarnrc.yml',
    # Auth tokens
    'token.json', '.netrc', '.htpasswd',
    # Database
    'database.yml', 'database.json',
    # Keychain / password managers
    'keychain.db', 'login.keychain-db',
    # Other
    '.git-credentials', '.docker/config.json',
}

# --- Blocked extensions ---
BLOCKED_EXTS = {
    '.pem', '.key', '.pfx', '.p12', '.p8',
    '.jks', '.keystore',       # Java keystores
    '.gpg', '.asc',            # GPG/PGP keys
    '.kdbx',                   # KeePass database
    '.1pux',                   # 1Password export
}

# --- Blocked directory patterns (anywhere in path) ---
BLOCKED_DIRS = {
    '.ssh',
    '.aws',
    '.gnupg',
    '.docker',
    '.kube',
    '.config/gcloud',
    '.terraform',
    'Keychain-DB',
}

# --- Blocked filename patterns (startswith / contains) ---
BLOCKED_PREFIXES = ('.env.',)
BLOCKED_CONTAINS = ('secret', 'credential', 'private_key', 'privatekey')

def is_blocked(filepath: str) -> tuple[bool, str]:
    """Check if a file path should be blocked. Returns (blocked, reason)."""
    p = Path(filepath)
    name = p.name
    ext = p.suffix.lower()

    # Exact filename match
    if name in BLOCKED_NAMES:
        return True, f"'{name}' is a known credentials file"

    # Extension match
    if ext in BLOCKED_EXTS:
        return True, f"'{ext}' files typically contain cryptographic keys"

    # Directory match — check if any blocked dir is in the path
    path_str = str(p)
    for blocked_dir in BLOCKED_DIRS:
        if f'/{blocked_dir}/' in path_str or path_str.endswith(f'/{blocked_dir}'):
            return True, f"'{blocked_dir}/' is a sensitive directory"

    # Prefix match (catches .env.anything)
    for prefix in BLOCKED_PREFIXES:
        if name.startswith(prefix):
            return True, f"'{name}' matches sensitive file pattern '{prefix}*'"

    # Contains match (catches files with 'secret' or 'credential' in the name)
    name_lower = name.lower()
    for keyword in BLOCKED_CONTAINS:
        if keyword in name_lower and ext in ('.json', '.yml', '.yaml', '.toml', '.xml', '.conf', '.cfg', '.ini', '.txt', ''):
            return True, f"'{name}' contains '{keyword}' and may hold sensitive data"

    # User-defined blocked paths (env var, colon-separated)
    custom_paths = os.environ.get('CLAUDE_BLOCKED_PATHS', '')
    if custom_paths:
        for blocked_path in custom_paths.split(':'):
            blocked_path = blocked_path.strip()
            if blocked_path and filepath.startswith(blocked_path):
                return True, f"path is inside user-blocked directory '{blocked_path}'"

    return False, ""

try:
    data = json.load(sys.stdin)
    fp = data.get('tool_input', {}).get('file_path', '')
    if not fp:
        sys.exit(0)

    blocked, reason = is_blocked(fp)
    if blocked:
        print(f"SECURITY BLOCK: {reason}. Use environment variables instead.", file=sys.stderr)
        sys.exit(2)

    sys.exit(0)
except Exception:
    sys.exit(0)
